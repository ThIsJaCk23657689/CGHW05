#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTextureCoords;

struct Material {
	vec4 ambient;
	vec4 diffuse;
	vec4 specular;
	vec4 emission;
	float shininess;

	sampler2D diffuse_texture;
	sampler2D specular_texture;
	sampler2D emission_texture;

	bool enableColorTexture;
    bool enableSpecularTexture;
	bool enableEmission;
    bool enableEmissionTexture;
};

struct Light {
	vec3 position;
	vec3 direction;
	
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	float constant;
	float linear;
	float quadratic;

	float cutoff;
	float outerCutoff;
	float exponent;

	bool enable;
	int caster;
};

// 0 Direction Light; 1 ~ 5 Point Light; 6 ~ 7 Spot Light;
#define NUM_LIGHTS 8

out VS_OUT {
	vec3 NaviePos;
	vec3 FragPos;
	vec3 Color;
	vec2 TexCoords;
} vs_out;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

uniform vec3 viewPos;
uniform bool useBlinnPhong;
uniform bool useSpotExponent;
uniform bool useLighting;

uniform bool isCubeMap;
uniform samplerCube skybox;

uniform Material material;
uniform Light lights[NUM_LIGHTS];

vec3 CalcLight(Light light, vec3 normal, vec3 viewDir) {

	vec3 ambient = vec3(0.0);
	vec3 diffuse = vec3(0.0);
	vec3 specular = vec3(0.0);

	vec3 lightDir = vec3(0.0);
	if (light.caster == 0) {
		// Direction Light
		lightDir = normalize(-light.direction);
	} else {
		lightDir = normalize(light.position - vs_out.FragPos);
	}

	float diff = max(dot(normal, lightDir), 0.0);

	float spec = 0.0;
	if (useBlinnPhong) {
		vec3 halfway = normalize(lightDir + viewDir);
		spec = pow(max(dot(normal, halfway), 0.0), material.shininess);
	} else {
		vec3 reflectDir = reflect(-lightDir, normal);
		spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
	}

	ambient = light.ambient;
	diffuse = light.diffuse * diff;
	specular = light.specular * spec;

	if (light.caster == 0) {
		// Direction Light
		if (isCubeMap) {
			ambient = light.diffuse;
			diffuse = light.diffuse;
			specular *= 0.0f;
		}
	} else {
		// Point Light or Spot Light
		if (isCubeMap) {
			ambient *= 0.0f;
			diffuse *= 0.0f;
			specular *= 0.0f;
		} else {
			float distance = length(light.position - vs_out.FragPos);
			float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

			ambient *= attenuation;
			diffuse *= attenuation;
			specular *= attenuation;

			if (light.caster == 2) {
				// Spot Light
				float intensity = 0.0f;
				if (useSpotExponent) {
					float theta = dot(lightDir, normalize(-light.direction));
					if (theta >= light.cutoff) {
						intensity = clamp(pow(theta, light.exponent), 0.0, 1.0);
					} else {
						intensity = 0.0f;
					}
				} else {
					float theta = dot(lightDir, normalize(-light.direction));
					float epsilon = light.cutoff - light.outerCutoff;
					intensity = clamp((theta - light.outerCutoff) / epsilon, 0.0, 1.0);
				}

				ambient *= intensity;
				diffuse *= intensity;
				specular *= intensity;
			}
		}
	}

	return ambient + diffuse + specular;
}

void main() {
	vs_out.NaviePos = aPosition;
	vs_out.FragPos =  vec3(model * vec4(aPosition, 1.0));
	vec3 Normal = mat3(transpose(inverse(model))) * aNormal;
	vs_out.TexCoords = aTextureCoords;
	
	if (isCubeMap) {
		// 繪製天空盒
		mat4 view_new = mat4(mat3(view));
		vec4 pos = projection * view_new * vec4(vs_out.FragPos, 1.0);
		gl_Position = pos.xyww;
	} else {
		gl_Position = projection * view * vec4(vs_out.FragPos, 1.0);
	}

	// 是否開啟光照
	if (useLighting) {
		// 計算光照
		vec3 norm = normalize(Normal);
		vec3 viewDir = normalize(viewPos - vs_out.FragPos);

		vec3 illumination = vec3(0.0f);
		for (int i = 0; i < NUM_LIGHTS; i++) {
			if (!lights[i].enable) {
				continue;
			}
			illumination += CalcLight(lights[i], norm, viewDir);
		}
		vs_out.Color = clamp(illumination, 0.0, 1.0);
	} else {
		vs_out.Color = vec3(1.0f);
	}
}