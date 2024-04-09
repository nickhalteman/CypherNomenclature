shader_type canvas_item;

uniform sampler3D textureNoise;
uniform sampler3D lineNoise;


uniform float slideSpeed : hint_range(0.0,1.0,0.01);
uniform bool reverse;
uniform float mutateSpeed : hint_range(0.0,1.0,0.01);
uniform float fillRatio : hint_range(0.0,1.0,0.01);

uniform float staticAmplitude : hint_range(0.0, 1.0, 0.01);
uniform float staticSpeed : hint_range(0.0, 1.0, 0.01);

varying flat vec2 origin;

void vertex(){
	origin = (MODEL_MATRIX * vec4(0.0,0.0,0.0,1.0)).xy;
}

void fragment() {
	
	float theta;
	
	
	if(reverse){
		theta = UV.x - TIME * slideSpeed + origin.x + origin.y * 5.0;
	} else {
		theta = UV.x + TIME * slideSpeed + origin.x + origin.y * 5.0;
	}
	
	vec2 sampleTimeCoords = vec2(cos(theta)*0.5 + 0.5,sin(theta)*0.5 + 0.5);
	float sample = texture(lineNoise,vec3(sampleTimeCoords,mod(TIME * mutateSpeed + origin.x * 5.0 + origin.y,1.0))).x;
	
	if(sample > fillRatio) {
		COLOR = vec4(0.0);
	} else {
		vec2 sampleCoord = vec2(mod(FRAGCOORD.x,256.0)/256.0,mod(FRAGCOORD.y,256.0)/256.0);
		float staticSample = texture(textureNoise,vec3(sampleCoord,mod(TIME*staticSpeed,1.0))).x;
		staticSample = 1.0 - staticSample * staticSample * staticAmplitude;
		COLOR *= vec4(vec3(1.0),staticSample);
		
	}
}
