#version 330

in vec3 vertexPosition;

uniform mat4 mvp;
uniform mat4 matModel;

out vec3 worldPos;

void main()
{
    vec4 world = matModel * vec4(vertexPosition, 1.0);
    worldPos = world.xyz;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}