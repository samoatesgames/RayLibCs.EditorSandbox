#version 330

in vec2 fragTexCoord;
in vec3 fragCameraPos;

out vec4 finalColor;

// config params
uniform float _GridBias = 1.0f;
uniform float _GridDiv = 10.0f;
uniform vec4 _BaseColor = vec4(0, 0, 0, 0.5);
uniform vec4 _LineColor = vec4(1, 1, 1, 1);
uniform float _LineWidth = 0.5;
uniform float _MajorLineWidth = 1.0f;
uniform float _GridHalfPlaneSize = 200.0f;

void main()
{
    // number of divisions in the grid
    float gridDiv = max(round(_GridDiv), 2.0);

    // distance to surface (or orth scale)
    float logLength = (0.5 * log(dot(fragCameraPos, fragCameraPos)))/log(gridDiv) - _GridBias;

    // get stepped log values
    float logA = floor(logLength);
    float logB = logA + 1.0;
    float blendFactor = fract(logLength);

    // scales for each UV set derived from log values
    float uvScaleA = pow(gridDiv, logA);
    float uvScaleB = pow(gridDiv, logB);

    // scale each UV
    vec2 UVA = fragTexCoord / uvScaleA;
    vec2 UVB = fragTexCoord / uvScaleB;

    // proceedural grid
    // we use a third UV set for the grid to show major grid lines thicker
    float logC = logA + 2.0;
    float uvScaleC = pow(gridDiv, logC);
    vec2 UVC = fragTexCoord / uvScaleC;

    // proceedural grid UVs sawtooth to triangle wave
    vec2 gridUVA = 1.0 - abs(fract(UVA) * 2.0 - 1.0);
    vec2 gridUVB = 1.0 - abs(fract(UVB) * 2.0 - 1.0);
    vec2 gridUVC = 1.0 - abs(fract(UVC) * 2.0 - 1.0);

    // adjust line width based on blend factor
    float lineWidthA = _LineWidth * (1-blendFactor);
    float lineWidthB = mix(_MajorLineWidth, _LineWidth, blendFactor);
    float lineWidthC = _MajorLineWidth * blendFactor;

    // fade lines out when below 1 pixel wide
    float lineFadeA = clamp(lineWidthA, 0, 1);
    float lineFadeB = clamp(lineWidthB, 0, 1);
    float lineFadeC = clamp(lineWidthC, 0, 1);

    // get screen space derivatives of base UV
    vec2 uvLength = max(vec2(length(vec2(dFdx(fragTexCoord.x), dFdy(fragTexCoord.x))), length(vec2(dFdx(fragTexCoord.y), dFdy(fragTexCoord.y)))), 0.00001);

    // calculate UV space width for anti-aliasing
    // This is done by scaling base UV rather than using derivatives of the pre-scaled UVs
    // to avoid artifacts at scale discontinuity edges. Note, this intentionally does not
    // into account the * 2.0 that was applied to the grid UVs so these are half derivs!
    vec2 lineAAA = uvLength / uvScaleA;
    vec2 lineAAB = uvLength / uvScaleB;
    vec2 lineAAC = uvLength / uvScaleC;

    // use smoothstep to get nice anti-aliasing on lines
    // line width * half deriv == 1.0 equals 1 pixel wide rather than 2 pixels wide
    // +/- 1.5 * half deriv == +/- 0.75 pixel AA
    vec2 grid2A = smoothstep((lineWidthA + 1.5) * lineAAA, (lineWidthA - 1.5) * lineAAA, gridUVA);
    vec2 grid2B = smoothstep((lineWidthB + 1.5) * lineAAB, (lineWidthB - 1.5) * lineAAB, gridUVB);
    vec2 grid2C = smoothstep((lineWidthC + 1.5) * lineAAC, (lineWidthC - 1.5) * lineAAC, gridUVC);

    // combine x and y grid lines together and apply < 1 pixel width fade
    float gridA = clamp(grid2A.x + grid2A.y, 0, 1) * lineFadeA;
    float gridB = clamp(grid2B.x + grid2B.y, 0, 1) * lineFadeB;
    float gridC = clamp(grid2C.x + grid2C.y, 0, 1) * lineFadeC;

    // combine all 3 grids together
    float grid = clamp(gridA + max(gridB, gridC), 0, 1);

    float camera_dist = distance(vec3(fragTexCoord.x, 0.0f, fragTexCoord.y), vec3(fragCameraPos.x, 0.0f, fragCameraPos.z));
    float dist_scalar = 1.0f - smoothstep(0.0f, _GridHalfPlaneSize, camera_dist);

    // mix between base and line color
    vec4 grid_color = mix(_BaseColor, _LineColor, grid * _LineColor.a);
    finalColor = vec4(grid_color.xyz, grid_color.w * dist_scalar);
}
