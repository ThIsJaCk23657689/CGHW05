#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTextureCoords;

out vec3 color;
out vec3 NaviePos;
out vec2 TextureCoords;

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

uniform vec3 viewPos;

uniform DirectionLight dirlight;
uniform PointLight pointlights[NUM_POINTLIGHTS];
uniform SpotLight spotlights[NUM_SPOTLIGHTS];
uniform Material material;

uniform bool useLighting;
uniform bool useBlinnPhong;
uniform bool useSpotExponent;

uniform bool isCubeMap;
uniform samplerCube skybox;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

vec3 CalcDirLight(DirectionLight light, vec3 normal, vec3 viewDir);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir);

void main() {
	NaviePos = aPosition;
	vec3 FragPos =  vec3(model * vec4(aPosition, 1.0));
	vec3 Normal = mat3(transpose(inverse(model))) * aNormal;
	TextureCoords = aTextureCoords;

	if (isCubeMap) {
		// 繪製天空盒
		mat4 view_new = mat4(mat3(view));
		vec4 pos = projection * view_new * vec4(FragPos, 1.0);
		gl_Position = pos.xyww;
	} else {
		gl_Position = projection * view * vec4(FragPos, 1.0);
	}

	// 是否開啟光照
	if (!useLighting) {
		color = vec3(1.0f);
	} else {
		
		// 計算光照
		vec3 norm = normalize(Normal);
		vec3 viewDir = normalize(viewPos - FragPos);
		vec3 illumination = vec3(0.0f);
		if (dirlight.enable) {
			illumination += clamp(CalcDirLight(dirlight, norm, viewDir), 0.0f, 1.0f);
		}
		for (int i = 0; i < NUM_POINTLIGHTS; i++) {
			if (!pointlights[i].enable) {
				continue;
			}
			illumination += clamp(CalcPointLight(pointlights[i], norm, FragPos, viewDir), 0.0f, 1.0f);
		}
		for (int i = 0; i < NUM_SPOTLIGHTS; i++) {
			if (!spotlights[i].enable) {
				continue;
			}
			illumination += clamp(CalcSpotLight(spotlights[i], norm, FragPos, viewDir), 0.0f, 1.0f);
		}

		color = clamp(illumination, 0.0, 1.0);
	}
}

vec3 CalcDirLight(DirectionLight light, vec3 normal, vec3 viewDir) {
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

	vec3 ambient = light.ambient;
	vec3 diffuse = light.diffuse * diff;
	vec3 specular = light.specular * spec;

	if (isCubeMap) {
		diffuse = light.diffuse;
		specular *= 0.0f;
	}

	return ambient + diffuse + specular;
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir){
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

	vec3 ambient = light.ambient;
	vec3 diffuse = light.diffuse * diff;
	vec3 specular = light.specular * spec;

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

vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir){
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

	vec3 ambient = light.ambient;
	vec3 diffuse = light.diffuse * diff;
	vec3 specular = light.specular * spec;

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