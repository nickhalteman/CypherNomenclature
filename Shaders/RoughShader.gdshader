shader_type canvas_item;

uniform vec4 color;

uniform sampler3D noise;

uniform float period : hint_range(0.0, 16.0, 0.05);
uniform float lineTint : hint_range(0.0, 1.0, 0.01);

uniform float staticAmplitude : hint_range(0.0, 1.0, 0.01);
uniform float staticSpeed : hint_range(0.0, 1.0, 0.01);

uniform float distortionAmplitude : hint_range(0.0, 10.0, 0.05);
uniform float distortionSpeed : hint_range(0.0, 1.0, 0.01);

uniform bool drawCenter;
uniform bool effectCenter;

uniform bool fadeBorder;

uniform float borderThickness : hint_range(0.0, 32.0, 1.0);

varying highp vec2 localPosition;

void vertex(){
	localPosition = VERTEX;
}

void fragment() {
	float fade = 1.0;
	bool isBorder = false;
	
	float hDist = -1.0;
	float vDist = -1.0;
	
	if(localPosition.x < borderThickness){
		hDist = borderThickness - localPosition.x;
		isBorder = true;
	}
	
	if(localPosition.y < borderThickness){
		vDist = borderThickness - localPosition.y;
		isBorder = true;
	}
	
	if(UV.x > 0.5){
		float rightDist = localPosition.x - (localPosition.x / UV.x - borderThickness);
		if(rightDist > 0.0){
			hDist = rightDist;
			isBorder = true;
		}
	}
	
	if(UV.y > 0.5){
		float bottomDist = localPosition.y - (localPosition.y / UV.y - borderThickness);
		if(bottomDist > 0.0){
			vDist = bottomDist;
			isBorder = true;
		}
	}
	
	if(vDist > 0.0 && hDist > 0.0){
		fade = 1.0 - sqrt(vDist * vDist + hDist * hDist) / borderThickness;
	} else if(vDist > 0.0){
		fade = 1.0 - vDist / borderThickness;
	} else if(hDist > 0.0){
		fade = 1.0 - hDist / borderThickness;
	}
	
	vec2 sampleCoord = vec2(mod(FRAGCOORD.x,256.0)/256.0,mod(FRAGCOORD.y,256.0)/256.0);
	
	float staticSample = texture(noise,vec3(sampleCoord,mod(TIME*staticSpeed,1.0))).x;
	staticSample = 1.0 - staticSample * staticSample * staticAmplitude;
	
	
	
	float distortionSample = texture(noise,vec3(sampleCoord,mod(TIME*distortionSpeed,1.0))).x * distortionAmplitude;
	
	if(!drawCenter && !isBorder){
		COLOR *= vec4(0.0);
	}
	
	COLOR *= color;
	
	if(effectCenter || isBorder){
		COLOR *= vec4(vec3(staticSample),1.0);
		if( mod(FRAGCOORD.y,period) < period / 2.0){
			COLOR *= vec4(vec3(1.0 - lineTint),1.0);
		}
	}
	
	
	
	if(fadeBorder){
		COLOR *= vec4(vec3(1.0),fade);
	}
	
}
