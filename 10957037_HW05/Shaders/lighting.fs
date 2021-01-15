#version 330 core
out vec4 FragColor;

struct Material {
	sampler2D diffuse_texture;
	sampler2D specular_texture;
	sampler2D emission_texture;

	vec4 diffuse;
	vec4 specular;
	vec4 emission;

	float shininess;
};

struct DirectionLight {
	vec3 direction;
	
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	bool enable;
};

struct PointLight {
	vec3 position;
	
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	float constant;
	float linear;
	float quadratic;

	bool enable;
};

struct SpotLight {
	vec3 position;
	vec3 direction;
	
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	float cutOff;
	float outerCutoff;
	float exponent;

	float constant;
	float linear;
	float quadratic;

	bool enable;
};

#define NUM_POINTLIGHTS 5
#define NUM_SPOTLIGHTS 2

in vec3 NaviePos;
in vec3 FragPos;
in vec3 Normal;
in vec2 TextureCoords;

uniform vec3 viewPos;

uniform DirectionLight dirlight;
uniform PointLight pointlights[NUM_POINTLIGHTS];
uniform SpotLight spotlights[NUM_SPOTLIGHTS];
uniform Material material;

uniform bool enableTexture;
uniform bool useLighting;
uniform bool useEmission;
uniform bool useBlinnPhong;
uniform bool useSpotExponent;

uniform bool isCubeMap;
uniform samplerCube skybox;

vec3 CalcDirLight(DirectionLight light, vec3 normal, vec3 viewDir, vec4 texel_diff, vec4 texel_spec);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec4 texel_diff, vec4 texel_spec);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec4 texel_diff, vec4 texel_spec);

void main() {
	
	vec4 texture_diffuse = vec4(0.0);
	vec4 texture_specular = vec4(0.0);

	if (isCubeMap) {
		// 材質類型採用 cubemap
        texture_diffuse = texture(skybox, normalize(NaviePos));
        texture_specular = texture_diffuse;
	} else {
		if (enableTexture) {
			// 使用材質繪圖
			texture_diffuse = texture(material.diffuse_texture, TextureCoords);
			texture_specular = texture(material.specular_texture, TextureCoords);
		} else {
			// 純色填滿
			texture_diffuse = material.diffuse;
			texture_specular = material.specular;
		}
	}

	// 是否開啟光照
	if (!useLighting) {
		FragColor = texture_diffuse;
	} else {
		
		// 計算光照
		vec3 norm = normalize(Normal);
		vec3 viewDir = normalize(viewPos - FragPos);
		vec3 illumination = vec3(0.0f);
		if (dirlight.enable) {
			illumination += clamp(CalcDirLight(dirlight, norm, viewDir, texture_diffuse, texture_specular), 0.0f, 1.0f);
		}
		for (int i = 0; i < NUM_POINTLIGHTS; i++) {
			if (!pointlights[i].enable) {
				continue;
			}
			illumination += clamp(CalcPointLight(pointlights[i], norm, FragPos, viewDir, texture_diffuse, texture_specular), 0.0f, 1.0f);
		}
		for (int i = 0; i < NUM_SPOTLIGHTS; i++) {
			if (!spotlights[i].enable) {
				continue;
			}
			illumination += clamp(CalcSpotLight(spotlights[i], norm, FragPos, viewDir, texture_diffuse, texture_specular), 0.0f, 1.0f);
		}

		// 開啟自發光
		if (useEmission) {
			if (enableTexture) {
				// 使用材質
				illumination += texture(material.emission_texture, TextureCoords).rgb * 0.5;
			} else {
				// 使用顏色
				illumination += texture_diffuse.rgb * 1.5;
			}
		}

		// 去透明
		if (texture_diffuse.a < 0.1) {
			discard;
		}

		FragColor = vec4(clamp(illumination, 0.0, 1.0), texture_diffuse.a);
	}
}

vec3 CalcDirLight(DirectionLight light, vec3 normal, vec3 viewDir, vec4 texel_diff, vec4 texel_spec) {
	vec3 lightDir = normalize(-light.direction);
	float diff = max(dot(normal, lightDir), 0.0);

	float spec = 0.0f;
	if (useBlinnPhong) {
		vec3 halfway = normalize(lightDir + viewDir);
		spec = pow(max(dot(normal, halfway), 0.0), material.shininess);
	} else {
		vec3 reflectDir = reflect(-lightDir, normal);
		spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
	}

	vec3 ambient = light.ambient * texel_diff.rgb;
	vec3 diffuse = light.diffuse * diff * texel_diff.rgb;
	vec3 specular = light.specular * spec * texel_spec.rgb;

	if (isCubeMap) {
		diffuse = light.diffuse * texel_diff.rgb;
		specular *= 0.0f;
	}

	return ambient + diffuse + specular;
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec4 texel_diff, vec4 texel_spec){
	vec3 lightDir = normalize(light.position - fragPos);
	float diff = max(dot(normal, lightDir), 0.0);
	
	float spec = 0.0f;
	if (useBlinnPhong) {
		vec3 halfway = normalize(lightDir + viewDir);
		spec = pow(max(dot(normal, halfway), 0.0), material.shininess);
	} else {
		vec3 reflectDir = reflect(-lightDir, normal);
		spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
	}

	float distance = length(light.position - fragPos);
	float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

	vec3 ambient = light.ambient * texel_diff.rgb;
	vec3 diffuse = light.diffuse * diff * texel_diff.rgb;
	vec3 specular = light.specular * spec * texel_spec.rgb;

	ambient *= attenuation;
	diffuse *= attenuation;
	specular *= attenuation;

	if (isCubeMap) {
		ambient *= 0.0f;
		diffuse *= 0.0f;
		specular *= 0.0f;
	}

	return ambient + diffuse + specular;
}

vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec4 texel_diff, vec4 texel_spec){
	vec3 lightDir = normalize(light.position - fragPos);
	float diff = max(dot(normal, lightDir), 0.0);
	
	float spec = 0.0f;
	if (useBlinnPhong) {
		vec3 halfway = normalize(lightDir + viewDir);
		spec = pow(max(dot(normal, halfway), 0.0), material.shininess);
	} else {
		vec3 reflectDir = reflect(-lightDir, normal);
		spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
	}

	float distance = length(light.position - fragPos);
	float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

	float intensity = 0.0f;
	if (useSpotExponent) {
		float theta = dot(lightDir, normalize(-light.direction));
		if (theta >= light.cutOff) {
			intensity = clamp(pow(theta, light.exponent), 0.0, 1.0);
		} else {
			intensity = 0.0f;
		}
	} else {
		float theta = dot(lightDir, normalize(-light.direction));
		float epsilon = light.cutOff - light.outerCutoff;
		intensity = clamp((theta - light.outerCutoff) / epsilon, 0.0, 1.0);
	}

	vec3 ambient = light.ambient * texel_diff.rgb;
	vec3 diffuse = light.diffuse * diff * texel_diff.rgb;
	vec3 specular = light.specular * spec * texel_spec.rgb;

	ambient *= attenuation * intensity;
	diffuse *= attenuation * intensity;
	specular *= attenuation * intensity;

	if (isCubeMap) {
		ambient *= 0.0f;
		diffuse *= 0.0f;
		specular *= 0.0f;
	}

	return ambient + diffuse + specular;
}