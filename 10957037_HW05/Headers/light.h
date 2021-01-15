#ifndef LIGHT_H
#define LIGHT_H

#include <string>
#include <glad/glad.h>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <vector>

enum Light_Caster {
	DIRECTION,
	POINT,
	SPOT
};

const glm::vec3 AMBIENT = glm::vec3(0.1f);
const glm::vec3 DIFFUSE = glm::vec3(0.7f);
const glm::vec3 SPECULAR = glm::vec3(0.4f);

const float LINEAR = 0.09f;
const float QUADRATIC = 0.0032f;

const float CUTOFF = 12.0f;
const float OUTERCUTOFF = 15.0f;
const float EXPONENT = 128.0f;

class Light
{
public:
	glm::vec3 Position;
	glm::vec3 Direction;
	
	glm::vec3 Ambient;
	glm::vec3 Diffuse;
	glm::vec3 Specular;
	
	float Constant;
	float Linear;
	float Quadratic;

	float Cutoff;
	float OuterCutoff;
	float Exponent;
	
	bool Enable;
	unsigned int Caster;

	// Direction Light
	Light(glm::vec4 direction = glm::vec4(0.0f, 0.1f, 0.1f, 0.0f), bool enable = true) : Ambient(AMBIENT), Diffuse(DIFFUSE), Specular(SPECULAR), Constant(1.0f), Linear(0.0f), Quadratic(0.0f), Cutoff(0.0f), OuterCutoff(0.0f), Exponent(0.0f) {
		Caster = Light_Caster::DIRECTION;
		Direction = glm::vec3(direction.x, direction.y, direction.z);
		Enable = enable;
	}

	// Point Light
	Light(glm::vec3 position = glm::vec3(0.0f, 0.0f, 0.0f), bool enable = true) : Ambient(AMBIENT), Diffuse(DIFFUSE), Specular(SPECULAR), Constant(1.0f), Linear(LINEAR), Quadratic(QUADRATIC), Cutoff(0.0f), OuterCutoff(0.0f), Exponent(0.0f) {
		Caster = Light_Caster::POINT;
		Position = position;
		Enable = enable;
	}

	// Spot Light
	Light(glm::vec3 position = glm::vec3(0.0f, 0.0f, 0.0f), glm::vec3 direction = glm::vec3(0.0f, 0.0f, -1.0f), bool enable = true) : Ambient(AMBIENT), Diffuse(DIFFUSE), Specular(SPECULAR), Constant(1.0f), Linear(LINEAR), Quadratic(QUADRATIC), Cutoff(CUTOFF), OuterCutoff(OUTERCUTOFF), Exponent(EXPONENT) {
		Caster = Light_Caster::SPOT;
		Position = position;
		Direction = direction;
		Enable = enable;
	}
private:
};

#endif // !LIGHT_H