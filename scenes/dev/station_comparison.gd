extends Control

const MODELS = [
	["Freeport (current)", "freeport_space_station1.glb"],
	["Gangut hub", "gangut_space_hub.glb"],
	["Sci-fi station 2", "sci-fi_space_station_2.glb"],
	["Black hole station", "black_hole_station_daily_draft_35.glb"],
]

func _ready() -> void:
	var background = ColorRect.new()
	background.color = Color(0.015, 0.02, 0.035)
	background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(background)
	var shader = Shader.new()
	shader.code = """
		shader_type spatial;
		render_mode cull_disabled;
		uniform sampler2D source_texture : source_color;
		uniform bool use_texture = false;
		uniform bool light_surface = false;
		void fragment() {
			vec3 source = use_texture ? texture(source_texture, UV).rgb : vec3(0.5);
			float tone = dot(source, vec3(0.299, 0.587, 0.114));
			ALBEDO = vec3(tone) * vec3(0.36, 0.4, 0.46);
			ROUGHNESS = 0.9;
			METALLIC = 0.05;
			EMISSION = light_surface ? vec3(0.05, 0.08, 0.1) : vec3(0.0);
		}
	"""
	for i in MODELS.size():
		create_panel(i, shader)

func create_panel(index: int, shader: Shader) -> void:
	var container = SubViewportContainer.new()
	container.position = Vector2((index % 2) * 960, (index / 2) * 540)
	container.size = Vector2(960, 500)
	add_child(container)
	var viewport = SubViewport.new()
	viewport.size = Vector2i(960, 500)
	viewport.own_world_3d = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	container.add_child(viewport)
	var world = Node3D.new()
	viewport.add_child(world)
	var environment = WorldEnvironment.new()
	environment.environment = Environment.new()
	environment.environment.background_mode = Environment.BG_COLOR
	environment.environment.background_color = Color(0.008, 0.012, 0.02)
	environment.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.environment.ambient_light_color = Color(0.3, 0.38, 0.5)
	environment.environment.ambient_light_energy = 0.12
	environment.environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	world.add_child(environment)
	var light = DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-20, -65, 0)
	light.light_color = Color(0.9, 0.55, 0.42)
	light.light_energy = 1.4
	world.add_child(light)
	var camera = Camera3D.new()
	camera.position = Vector3(0, 0, 12)
	camera.fov = 50
	world.add_child(camera)
	camera.look_at(Vector3.ZERO)
	var packed = load("res://assets/models/spaceobjects/" + MODELS[index][1]) as PackedScene
	if packed == null:
		push_error("Could not load station comparison model: " + MODELS[index][1])
		get_tree().quit(1)
		return
	var model = packed.instantiate() as Node3D
	var holder = Node3D.new()
	world.add_child(holder)
	holder.add_child(model)
	var meshes = model.find_children("*", "MeshInstance3D", true, false)
	var bounds = AABB()
	var first = true
	for part: MeshInstance3D in meshes:
		var local_bounds = part.mesh.get_aabb()
		for corner in 8:
			var point = part.global_transform * local_bounds.get_endpoint(corner)
			if first:
				bounds = AABB(point, Vector3.ZERO)
				first = false
			else:
				bounds = bounds.expand(point)
		for surface in part.mesh.get_surface_count():
			var original = part.get_active_material(surface) as BaseMaterial3D
			var material = ShaderMaterial.new()
			material.shader = shader
			if original != null:
				material.set_shader_parameter("use_texture", original.albedo_texture != null)
				if original.albedo_texture != null:
					material.set_shader_parameter("source_texture", original.albedo_texture)
				material.set_shader_parameter("light_surface", original.emission_enabled)
			part.set_surface_override_material(surface, material)
	if first or bounds.get_longest_axis_size() <= 0:
		push_error("Station comparison model has no usable geometry.")
		get_tree().quit(1)
		return
	model.position -= bounds.get_center()
	holder.scale = Vector3.ONE * (8.0 / bounds.get_longest_axis_size())
	var rotation = Basis.IDENTITY
	match bounds.get_longest_axis_index():
		1:
			rotation = Basis(Vector3.FORWARD, PI / 2)
		2:
			rotation = Basis(Vector3.UP, PI / 2)
	holder.basis = Basis(Vector3.UP, deg_to_rad(25)) * Basis(Vector3.RIGHT, deg_to_rad(12)) * rotation * Basis.from_scale(holder.scale)
	var title = Label.new()
	title.position = container.position + Vector2(20, 12)
	title.text = MODELS[index][0]
	title.add_theme_font_size_override("font_size", 26)
	add_child(title)
	var caption = Label.new()
	caption.position = container.position + Vector2(20, 503)
	caption.text = "Equal size / same lighting / intact geometry study"
	caption.add_theme_font_size_override("font_size", 18)
	add_child(caption)
