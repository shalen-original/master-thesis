pub mod keybindings {

    pub struct Context<'a> {
        pub current_increment_amount: f32,
        pub current_game_object: &'a str
    }

    pub struct KeyBinding {
        pub key: char,
        pub description: String,
        pub build: Box<dyn Fn(&mut Context) -> Option<String>>
    }

    pub fn build_key_bindings_vector() -> Vec<KeyBinding> {
        let mut v = Vec::new();

        v.push(KeyBinding{
            key: '+',
            description: "Increment amount".to_string(),
            build: Box::new(|ctx| {
                ctx.current_increment_amount += 0.001;
                Option::None
            })
        });

        v.push(KeyBinding{
            key: '-',
            description: "Descrement amount".to_string(),
            build: Box::new(|ctx| {
                ctx.current_increment_amount -= 0.001;
                if ctx.current_increment_amount < 0.0 {
                    ctx.current_increment_amount = 0.0;
                };
                Option::None
            })
        });

        v.push(KeyBinding{
            key: '0',
            description: "Select Unity 'DA42-1'".to_string(),
            build: Box::new(|ctx| {
                ctx.current_game_object = "DA42-1";
                Option::None
            })
        });

        v.push(KeyBinding{
            key: '1',
            description: "Select Unity 'DA42-2'".to_string(),
            build: Box::new(|ctx| {
                ctx.current_game_object = "DA42-2";
                Option::None
            })
        });

        v.push(KeyBinding{
            key: '2',
            description: "Select Unity 'Plane'".to_string(),
            build: Box::new(|ctx| {
                ctx.current_game_object = "Plane";
                Option::None
            })
        });

        v.push(KeyBinding{
            key: '3',
            description: "Select Unity 'ENUOrigin'".to_string(),
            build: Box::new(|ctx| {
                ctx.current_game_object = "ENUOrigin";
                Option::None
            })
        });

        v.push(KeyBinding{
            key: '4',
            description: "Select Unity 'SimulatorViewProjectionPoint'".to_string(),
            build: Box::new(|ctx| {
                ctx.current_game_object = "SimulatorViewProjectionPoint";
                Option::None
            })
        });

        v.push(KeyBinding{
            key: '5',
            description: "Select Unity 'CylinderRadiusMarker'".to_string(),
            build: Box::new(|ctx| {
                ctx.current_game_object = "CylinderRadiusMarker";
                Option::None
            })
        });

        v.push(KeyBinding{
            key: '6',
            description: "Select Unity 'SimulatorCylinderCenter'".to_string(),
            build: Box::new(|ctx| {
                ctx.current_game_object = "SimulatorCylinderCenter";
                Option::None
            })
        });

        v.push(KeyBinding{
            key: 'w',
            description: "Change translation, increment z".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, true, false, "z"))
        });

        v.push(KeyBinding{
            key: 's',
            description: "Change translation, decrement z".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, true, true, "z"))
        });

        v.push(KeyBinding{
            key: 'd',
            description: "Change translation, increment x".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, true, false, "x"))
        });

        v.push(KeyBinding{
            key: 'a',
            description: "Change translation, decrement x".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, true, true, "x"))
        });

        v.push(KeyBinding{
            key: 'q',
            description: "Change translation, increment y".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, true, false, "y"))
        });

        v.push(KeyBinding{
            key: 'e',
            description: "Change translation, decrement y".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, true, true, "y"))
        });

        v.push(KeyBinding{
            key: 'u',
            description: "Change rotation, increment z".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, false, false, "z"))
        });

        v.push(KeyBinding{
            key: 'j',
            description: "Change rotation, decrement z".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, false, true, "z"))
        });

        v.push(KeyBinding{
            key: 'k',
            description: "Change rotation, increment x".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, false, false, "x"))
        });

        v.push(KeyBinding{
            key: 'h',
            description: "Change rotation, decrement x".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, false, true, "x"))
        });

        v.push(KeyBinding{
            key: 'i',
            description: "Change rotation, increment y".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, false, false, "y"))
        });

        v.push(KeyBinding{
            key: 'y',
            description: "Change rotation, decrement y".to_string(),
            build: Box::new(|ctx| change_obj_command(ctx, false, true, "y"))
        });

        v.push(KeyBinding{
            key: 'z',
            description: "Dump data".to_string(),
            build: Box::new(|ctx| {
                Some(format!( r#"{{"type": "DUMP_UNITY_OBJECT_STATUS", "objectName": "{}"}}"#, ctx.current_game_object))
            })
        });

        v.push(KeyBinding{
            key: 'x',
            description: "Dump hierarchy".to_string(),
            build: Box::new(|_| {
                Some(r#"{"type": "DUMP_HIERARCHY"}"#.to_string())
            })
        });

        v
    }

    fn change_obj_command(ctx: &Context, translation: bool, decrement: bool, axis: &str) -> Option<String>{
        let kind = if translation { "TRANSLATION" } else { "ROTATION" };
        let a = if translation { ctx.current_increment_amount } else { ctx.current_increment_amount * 10.0 };
        let amount = if decrement { -a } else { a };

        Option::Some(format!(
            r#"{{"type": "MOVE_UNITY_OBJECT", "objectName": "{}", "kind": "{}", "axis": "{}", "amount": {}}}"#, 
            ctx.current_game_object, kind, axis, amount
        ))
    }
}