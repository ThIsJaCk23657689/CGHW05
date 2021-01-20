#version 330 core
out vec4 FragColor;

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

struct Fog {
	int mode;
	int depthType;
	float density;
	float f_start;
	float f_end;
	bool enable;
	vec4 color;
};

in VS_OUT {
	vec3 NaviePos;
	vec3 FragPos;
	vec3 Color;
	vec2 TexCoords;
} fs_in;

uniform vec3 viewPos;
uniform bool useLighting;
uniform bool useDiffuseTexture;
uniform bool useSpecularTexture;
uniform bool useEmission;
uniform bool useGamma;
uniform float GammaValue;

uniform bool isCubeMap;
uniform samplerCube skybox;

uniform Material material;
uniform Fog fog;

void main() {

	vec4 texel_diffuse = vec4(0.0);
	if (isCubeMap) {
		// 繪製天空盒
		texel_diffuse = texture(skybox, normalize(fs_in.NaviePos));
	} else if (useDiffuseTexture && material.enableColorTexture) {
		// 如果有開啟顯示材質 且 該物體有材質貼圖時 => 對圖片取樣
		texel_diffuse = texture(material.diffuse_texture, fs_in.TexCoords);
	} else {
		// 純色填滿
		texel_diffuse = material.diffuse;
	}

	vec3 texel = vec3(0.0);
	texel = texel_diffuse.rgb * fs_in.Color;

	// 開啟自發光
	if (useEmission && material.enableEmission) {
		if (material.enableEmissionTexture) {
			// 使用材質
			texel += texture(material.emission_texture, fs_in.TexCoords).rgb;
		} else {
			// 使用顏色
			texel += texel_diffuse.rgb * 1.5;
		}
	}

	// 去透明
	if (texel_diffuse.a < 0.1) {
		discard;
	}

	if (useLighting) {
		// Foggy Effect
		vec4 PreColor = vec4(clamp(texel, 0.0, 1.0), texel_diffuse.a);
		vec4 FinalColor = vec4(0.0);
		float distance = 0.0;
		float fogFactor = 0.0;

		if (fog.depthType == 0) {
			// Plane Based
			distance = abs((viewPos - fs_in.FragPos).z);
		} else {
			// Range Based
			distance = length(viewPos - fs_in.FragPos);
		}

		if (fog.enable) {
			if (fog.mode == 0) {
			// Foggy Effect Linear
			fogFactor = clamp((fog.f_end - distance) / (fog.f_end - fog.f_start), 0.0, 1.0);
			} else if (fog.mode == 1) {
				// Foggy Effect EXP
				fogFactor = clamp(1.0 / exp(fog.density * distance), 0.0, 1.0);
			} else if (fog.mode == 2) {
				// Foggy Effect EXP2
				fogFactor = clamp(1.0 / exp(fog.density * distance * distance), 0.0, 1.0);
			}
			FinalColor = mix(fog.color, PreColor, fogFactor);
		} else {
			// Close Foggy Effect
			FinalColor = PreColor;
		}

		// 迦瑪校正
		if (useGamma) {
			FinalColor = vec4(pow(FinalColor.xyz, vec3(GammaValue)), FinalColor.w);
		}

		FragColor = FinalColor;
	} else {
		FragColor = vec4(texel, texel_diffuse.a);
	}
}