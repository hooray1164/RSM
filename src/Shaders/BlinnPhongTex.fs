
in FragData {
	vec3 position;
	vec3 normal; 
	vec2 texCoords;
	vec4 lightSpacePosition;
} vsIn;

struct Light {
	vec3 position;
	vec3 direction;
	vec3 emission;
	float intensity;
	float linear;
	float quadratic;
	int type;
	bool state;
	float cutoff;
};

uniform cameraBlock {
   	mat4 ViewMatrix;
	mat4 ProjMatrix;
	mat4 ViewProjMatrix;
	vec3 ViewPos;
};

uniform Light lights[NUM_LIGHTS];

//Material parameters
uniform vec3 ambient;
uniform vec3 diffuse;
uniform vec3 specular;
uniform float shininess;

uniform sampler2D diffuseTex;

uniform sampler2D shadowMap;

/** /
// for specular maps
float fetchParameter(sampler2D samp){
	return texture(samp, vsIn.texCoords).r;
}

vec3 fetchNormal(){
	// normal map [0,1] to [-1,1]
	return normalize((texture(normalMap, vsIn.texCoords).rgb) * 2.0 - 1.0);
}
/**/

vec3 fetchDiffuse(){
	vec4 texel = texture(diffuseTex, vsIn.texCoords);
	if (texel.a <= 0.0)
		discard;
	return texel.rgb;
}

float shadowFactor(vec4 lightSpacePosition, vec3 N, vec3 L){
	// perform perspective divide: clip space-> normalized device coords (done automatically for gl_Position)
    vec3 projCoords = lightSpacePosition.xyz / lightSpacePosition.w;
    // bring from [-1,1] to [0,1]
    projCoords = projCoords * 0.5 + 0.5;

    float closestDepth = texture(shadowMap, projCoords.xy).r;
    float currentDepth = projCoords.z; 

    // shadow bias to reduce shadow acne
    float bias = max(0.05 * (1.0 - dot(N,L)), 0.005);
    float shadow = currentDepth - bias > closestDepth  ? 0.0 : 1.0;
    return shadow;
}

/* ==============================================================================
        Stage Outputs
 ============================================================================== */

out vec4 outColor;

void main(void) {

	/**/
	vec3 V = normalize(ViewPos - vsIn.position);
	vec3 N = vsIn.normal;
	vec3 L;

	vec3 retColor = vec3(0.0);

	for(int i=0; i < NUM_LIGHTS; i++){

		if (lights[i].type == 0){
			L = normalize(-lights[i].direction);
		}
		else {
			L = normalize(lights[i].position - vsIn.position);
		}

		if (lights[i].type == 2){
			float theta = dot(L, normalize(-lights[i].direction));
			if(theta <= lights[i].cutoff){
				continue;
			}
		}

		vec3 H = normalize(L + V);

		float NdotL = max(dot(N, L), 0.0);
		float NdotH = max(dot(N, H), 0.0);

		vec3 diff = vec3(0.0);
		vec3 spec = vec3(0.0);

		if (NdotL > 0.0){

			diff = lights[i].emission * lights[i].intensity * ( fetchDiffuse() * NdotL);
			spec = lights[i].emission * lights[i].intensity * ( specular * pow(NdotH, shininess));

			// if not directional light
			if (lights[i].type != 0){
				float distance = length(lights[i].position - vsIn.position);
				float attenuation = 1.0 / (1.0 + lights[i].linear * distance + lights[i].quadratic * pow(distance, 2.0));

				diff *= attenuation;
				spec *= attenuation;
			}
		}

		retColor += diff + spec;
	}

	outColor = vec4(retColor, 1.0);
	/**/
}