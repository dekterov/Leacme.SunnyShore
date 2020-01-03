// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using Godot;
using System;

public class Main : Spatial {

	public AudioStreamPlayer Audio { get; } = new AudioStreamPlayer();

	private Spatial boat = (Spatial)GD.Load<PackedScene>("res://scenes/Boat.tscn").Instance();

	private void InitSound() {
		if (!Lib.Node.SoundEnabled) {
			AudioServer.SetBusMute(AudioServer.GetBusIndex("Master"), true);
		}
	}

	public override void _Notification(int what) {
		if (what is MainLoop.NotificationWmGoBackRequest) {
			GetTree().ChangeScene("res://scenes/Menu.tscn");
		}
	}

	public override void _Ready() {
		var env = GetNode<WorldEnvironment>("sky").Environment;
		env.BackgroundMode = Godot.Environment.BGMode.Sky;
		env.BackgroundSky = new PanoramaSky() { Panorama = ((Texture)GD.Load("res://assets/water.hdr")) };
		env.BackgroundSkyRotationDegrees = new Vector3(25, 145, 30);

		var seaAudio = GD.Load<AudioStream>("res://assets/sea.ogg");
		seaAudio.Play(Audio);
		Audio.Seek((float)GD.RandRange(0, seaAudio.GetLength()));

		var sun1 = new DirectionalLight() {
			LightEnergy = 0.4f
		};
		var sun2 = new DirectionalLight() {
			LightEnergy = 0.4f,
			LightIndirectEnergy = 0
		};
		sun2.RotationDegrees = new Vector3(-62, -145, 0);
		var sun3 = new DirectionalLight() {
			LightEnergy = 0.4f,
		};
		sun3.RotationDegrees = new Vector3(-28, -105, 0);
		AddChild(sun1);
		AddChild(sun2);
		AddChild(sun3);

		InitSound();
		AddChild(Audio);

		var wMat = new ShaderMaterial() {
			Shader = new Shader() {
				Code = @"
					shader_type spatial;

					render_mode blend_mix,depth_draw_opaque,cull_back,diffuse_burley,specular_schlick_ggx;

					uniform vec4 albedo : hint_color;
					uniform sampler2D texture_albedo : hint_albedo;

					uniform float freq = -3.0;
					uniform float range = 0.05;

					float wave(vec2 vert, float t) {
						return sin(vert.x * freq + t) * range + sin(vert.y * freq / 2.0 + t) * range;
					}

					void vertex() {
						VERTEX.y += wave(VERTEX.xz, TIME);
						TANGENT = normalize(vec3(0f, wave(VERTEX.xz + vec2(0f, 0.2), TIME) - wave(VERTEX.xz + vec2(0f, -0.2), TIME), 0.4));
						BINORMAL = normalize(vec3(0.4, wave(VERTEX.xz + vec2(0.2, 0f), TIME) - wave(VERTEX.xz + vec2(-0.2, 0f), TIME ), 0f));
						NORMAL = cross(TANGENT, BINORMAL);
					}

					void fragment() {
						vec2 base_uv = UV;
						vec4 albedo_tex = texture(texture_albedo,base_uv);
						ALBEDO = albedo.rgb * albedo_tex.rgb;
						METALLIC = 1f;
						ROUGHNESS = 0f;
						SPECULAR = 1f;
						ALPHA = albedo.a * albedo_tex.a;
					}"
			}
		};
		wMat.SetShaderParam("albedo", new Color("aaeeffff"));

		AddChild(boat);
		boat.GetNode<MeshInstance>("Boat").MaterialOverride = new SpatialMaterial() {
			AlbedoColor = Colors.White
		};
		// boat.Scale *= 2f;
		boat.Translate(new Vector3(3.4f, -3.8f, -0.1f));
		boat.RotateY(Mathf.Deg2Rad(90));
		boat.RotateX(Mathf.Deg2Rad(-4));

		var wmi = new MeshInstance() {
			Mesh = new PlaneMesh() {
				Size = new Vector2(11, 11),
				SubdivideDepth = 120,
				SubdivideWidth = 120,
				Material = wMat
			}
		};
		AddChild(wmi);
		wmi.Translate(new Vector3(0, -2, 0));
		wmi.RotationDegrees = new Vector3(0, 0, -27);

	}

	float range = 0.2f;
	float freq = 1f;
	float time = 0f;

	public override void _Process(float delta) {
		time += delta;
		boat.Translate(new Vector3(0, (float)(range * Math.Sin(time * freq) / 100f), 0));
		boat.RotateX((float)(range * Math.Sin(time * freq) / 300f));
	}

}
