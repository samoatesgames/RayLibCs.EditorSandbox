#version 330

in vec3 worldPos;
out vec4 finalColor;

uniform vec3 cameraPos;

uniform float timeOfDay;
uniform vec3 dayColor;
uniform vec3 nightColor;
uniform vec3 horizonColor;

uniform float starIntensity;
uniform float time;

#define PI 3.14159265359

float hash(vec3 p)
{
    p = fract(p * 0.3183099 + vec3(0.1,0.2,0.3));
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

void main()
{
    // Correct view direction
    vec3 viewDir = normalize(worldPos - cameraPos);

    float sunAngle = (timeOfDay - 0.25) * PI * 2.0;
    vec3 sunDir = normalize(vec3(cos(sunAngle), sin(sunAngle), 0.0));
    vec3 moonDir = -sunDir;

    float sunHeight = sunDir.y;
    float dayFactor = clamp((sunHeight + 0.1) * 4.0, 0.0, 1.0);

    float y = clamp(viewDir.y * 0.5 + 0.5, 0.0, 1.0);

    vec3 daySky = mix(
        dayColor * 1.4,
        dayColor * 0.35,
        pow(y, 0.35)
    );

    float sunGlow = clamp(dot(viewDir, sunDir), 0.0, 1.0);
    sunGlow = pow(sunGlow, 8.0);

    daySky += vec3(1.0, 0.9, 0.6) * sunGlow * 0.25;

    vec3 skyZenith = mix(nightColor, daySky, dayFactor);

    float horizon = pow(1.0 - abs(viewDir.y), 4.0);
    float sunset = pow(1.0 - abs(sunHeight), 6.0);

    // only where sun is
    float sunSide = clamp(dot(viewDir, sunDir), 0.0, 1.0);

    vec3 sky = mix(skyZenith, horizonColor, horizon * sunset * sunSide);

    // Sun
    float sunDot = dot(viewDir, sunDir);
    float sun = smoothstep(0.999, 1.0, sunDot);
    sky += vec3(1.0, 0.9, 0.6) * sun * 5.0;

    // Moon
    float moonDot = dot(viewDir, moonDir);
    float moon = smoothstep(0.99975, 1.0, moonDot);
    sky += vec3(0.6, 0.7, 1.0) * moon * 100.0 * (1.0 - dayFactor);

    // Stars
    vec3 starDir = normalize(viewDir);
    // quantize to stable grid
    vec3 cell = floor(starDir * 400.0);
    float star = hash(cell);
    star = step(0.995, star);
    star *= (1.0 - dayFactor);
    star *= starIntensity;

    sky += vec3(star);

    finalColor = vec4(sky, 1.0);
}