#pragma once
#include <string>
#include <glad/glad.h>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <vector>

enum Fog_Mode {
	FOG_LINEAR,
	FOG_EXP,
	FOG_EXP2
};

enum Fog_DepthType {
	PLANE_BASED,
	RANGE_BASED
};

const float DENSITY = 0.15f;

class Fog
{
public:
	unsigned int Mode;
	unsigned int DepthType;
	float Density;
	float F_start;
	float F_end;
	bool Enable;
	glm::vec4 Color;

	Fog(glm::vec4 color = glm::vec4(1.0f, 1.0f, 1.0f, 1.0f), bool enable = true, float f_start = 20.0f, float f_end = 80.0f) : Density(DENSITY) {
		Mode = Fog_Mode::FOG_EXP;
		DepthType = Fog_DepthType::RANGE_BASED;
		F_start = f_start;
		F_end = f_end;
		Enable = enable;
		Color = color;
	}
};