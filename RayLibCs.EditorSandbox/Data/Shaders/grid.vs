#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec4 vertexColor;

// Output to fragment shader
out vec2 fragTexCoord;
out vec3 fragCameraPos;

uniform mat4 mvp; // model-view-projection matrix
uniform mat4 modelWorldTransform; // model-view-projection matrix
uniform vec3 cameraPos;

void main()
{
    vec3 worldPos = (modelWorldTransform * vec4(vertexPosition, 1.0)).xyz;
    fragTexCoord = worldPos.xz;
    fragCameraPos = cameraPos;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
