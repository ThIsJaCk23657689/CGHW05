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

in vec3 color;
in vec3 NaviePos;
in vec2 TextureCoords;

uniform bool enableTexture;
uniform bool useEmission;
uniform Material material;

uniform bool isCubeMap;
uniform samplerCube skybox;

void main() {
	
	vec4 texture_diffuse = vec4(0.0);
	vec4 texture_specular = vec4(0.0);
	if (isCubeMap) {
		// 繪製天空盒
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

	vec3 texel = texture_diffuse.rgb * color;

	// 開啟自發光
	if (useEmission) {
		if (enableTexture) {
			// 使用材質
			texel += texture(material.emission_texture, TextureCoords).rgb * 0.5;
		} else {
			// 使用顏色
			texel += texture_diffuse.rgb * 1.5;
		}
	}

	// 去透明
	if (texture_diffuse.a < 0.1) {
		discard;
	}

	FragColor = vec4(texel, texture_diffuse.a);
}