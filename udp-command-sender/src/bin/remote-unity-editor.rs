extern crate crossterm;

use std::net::UdpSocket;
use std::io::{stdout, Write};
use std::convert::TryFrom;

use crossterm::QueueableCommand;
use crossterm::cursor;
use crossterm::event::{read, Event, KeyCode, KeyEvent};
use crossterm::style::Print;
use crossterm::terminal;

use udp_command_sender::keybindings::{Context, KeyBinding, build_key_bindings_vector};

fn print_preamble(ctx: &Context, bindings: &[KeyBinding]) -> crossterm::Result<()> {
    stdout()
        .queue(terminal::Clear(terminal::ClearType::All))?
        .queue(cursor::MoveTo(0, 0))?
        .queue(Print("Remote Hololens Controller"))?
        .queue(cursor::MoveTo(0, 1))?
        .queue(Print("Context:"))?
        .queue(cursor::MoveTo(1, 2))?
        .queue(Print(format!("Current translation amount: {}", ctx.current_increment_amount)))?
        .queue(cursor::MoveTo(1, 3))?
        .queue(Print(format!("Current game object: {}", ctx.current_game_object)))?
        .queue(cursor::MoveTo(0, 4))?
        .queue(Print("Available commands: "))?
        .queue(cursor::MoveTo(1, 5))?
        .queue(Print("[p] Quit"))?;

    for (i, kb) in bindings.iter().enumerate() {
        stdout()
            .queue(cursor::MoveTo(1, u16::try_from(i).unwrap() + 6))?
            .queue(Print(format!("[{}] {}", kb.key, kb.description)))?;
    }

    stdout()
        .queue(cursor::MoveTo(1, u16::try_from(bindings.len()).unwrap() + 6))?;
    
    stdout().flush()?;

    Ok(())
}


fn main() -> crossterm::Result<()> {
    let socket_hololens = UdpSocket::bind("0.0.0.0:0")?;
    socket_hololens.connect("192.168.0.200:53941")?;

    let socket_unity = UdpSocket::bind("0.0.0.0:0")?;
    socket_unity.connect("127.0.0.1:53941")?;

    let bindings = build_key_bindings_vector();
    let mut ctx = Context { 
        current_increment_amount: 0.001, 
        current_game_object: "Plane"
    };

    crossterm::terminal::enable_raw_mode()?;

    print_preamble(&ctx, &bindings)?;

    loop {
        let event = read().unwrap();

        if let Event::Key(KeyEvent{code, modifiers: _}) = event {

            if code == KeyCode::Char('p') {
                break;
            }

            for kb in &bindings {
                if code == KeyCode::Char(kb.key){
                    let s = (kb.build)(&mut ctx);

                    if let Some(almost_cmd) = s {
                        let cmd = almost_cmd + "\n";
                        socket_hololens.send(cmd.as_bytes())?;
                        socket_unity.send(cmd.as_bytes())?;
                    }
                    
                }
            }

            print_preamble(&ctx, &bindings)?;
        }
    }

    crossterm::terminal::disable_raw_mode()?;
    Ok(())
}