extern crate crossterm;

use std::net::UdpSocket;
use std::io::{stdout, Write};

use crossterm::QueueableCommand;
use crossterm::cursor;
use crossterm::event::{read, Event, KeyCode, KeyEvent};
use crossterm::style::Print;
use crossterm::terminal;

fn print_preamble(curr: &SimStatus) -> crossterm::Result<()> {
    stdout()
        .queue(terminal::Clear(terminal::ClearType::All))?
        .queue(cursor::MoveTo(0, 0))?
        .queue(Print("Simulator Position Updater"))?
        .queue(cursor::MoveTo(0, 1))?
        .queue(Print(format!("Current position (lat/lon/alt): {}° {}° {}m", curr.lat_degrees, curr.lon_degrees, curr.alt_meters)))?
        .queue(cursor::MoveTo(0, 2))?
        .queue(Print(format!("Current orientation (yaw/pitch/roll): {}° {}° {}°", curr.yaw_degrees, curr.pitch_degrees, curr.roll_degrees)))?
        .queue(cursor::MoveTo(0, 3))?
        .queue(Print("Change position with [w][a][s][d][q][e]"))?
        .queue(cursor::MoveTo(0, 4))?
        .queue(Print("Change orientation with [i][k][j][l][u][o]"))?
        .queue(cursor::MoveTo(0, 5))?
        .queue(Print("Move to EDDM with [1], move to LOWI with [2], move to tunnel with [3]"))?
        .queue(cursor::MoveTo(0, 6))?
        .queue(Print("Exit with [p]"))?
        .queue(cursor::MoveTo(0, 7))?
        .flush()?;

    Ok(())
}

#[derive(Clone, Copy)]
struct SimStatus {
    pub lat_degrees: f64,
    pub lon_degrees: f64,
    pub alt_meters: f64,
    pub yaw_degrees: f64,
    pub pitch_degrees: f64,
    pub roll_degrees: f64
}

static EDDM: SimStatus = SimStatus {
    lat_degrees: 48.361961,
    lon_degrees: 11.750353,
    alt_meters: 700.0,
    yaw_degrees: 82.0,
    roll_degrees: 0.0,
    pitch_degrees: 0.0
};

static LOWI: SimStatus = SimStatus {
    lat_degrees: 47.25899,
    lon_degrees: 11.33090,
    alt_meters: 700.0,
    yaw_degrees: 84.0,
    roll_degrees: 0.0,
    pitch_degrees: 0.0
};

static TUNNEL: SimStatus = SimStatus {
    lat_degrees: 48.33842534,
    lon_degrees: 11.45679864,
    alt_meters: 5101.0 / 3.0,
    yaw_degrees: 84.0,
    roll_degrees: 0.0,
    pitch_degrees: 0.0
};

fn main() -> crossterm::Result<()> {
    let socket_hololens = UdpSocket::bind("0.0.0.0:0")?;
    socket_hololens.connect("192.168.0.200:53941")?;

    let socket_unity = UdpSocket::bind("0.0.0.0:0")?;
    socket_unity.connect("127.0.0.1:53941")?;

    let socket_python = UdpSocket::bind("0.0.0.0:0")?;
    socket_python.connect("127.0.0.1:53943")?;

    let mut current_sim_status = EDDM;

    crossterm::terminal::enable_raw_mode()?;

    print_preamble(&current_sim_status)?;

    loop {
        let event = read().unwrap();

        if let Event::Key(KeyEvent{code, modifiers: _}) = event {

            if code == KeyCode::Char('p') {
                break;
            }

            match code {
                KeyCode::Char('1') => current_sim_status = EDDM,
                KeyCode::Char('2') => current_sim_status = LOWI,
                KeyCode::Char('3') => current_sim_status = TUNNEL,
                KeyCode::Char('w') => current_sim_status.lat_degrees   += 0.0005,
                KeyCode::Char('s') => current_sim_status.lat_degrees   -= 0.0005,
                KeyCode::Char('d') => current_sim_status.lon_degrees   += 0.0005,
                KeyCode::Char('a') => current_sim_status.lon_degrees   -= 0.0005,
                KeyCode::Char('e') => current_sim_status.alt_meters    += 10.0,
                KeyCode::Char('q') => current_sim_status.alt_meters    -= 10.0,
                KeyCode::Char('i') => current_sim_status.yaw_degrees   += 1.0,
                KeyCode::Char('k') => current_sim_status.yaw_degrees   -= 1.0,
                KeyCode::Char('l') => current_sim_status.pitch_degrees += 1.0,
                KeyCode::Char('j') => current_sim_status.pitch_degrees -= 1.0,
                KeyCode::Char('o') => current_sim_status.roll_degrees  += 1.0,
                KeyCode::Char('u') => current_sim_status.roll_degrees  -= 1.0,
                _ => ()
            }

            let deg_to_rad = 3.1415926 / 180.0;
            let mut cmd = vec![00u8];
            cmd.extend((current_sim_status.lat_degrees * deg_to_rad).to_le_bytes());
            cmd.extend((current_sim_status.lon_degrees * deg_to_rad).to_le_bytes());
            cmd.extend(current_sim_status.alt_meters.to_le_bytes());
            cmd.extend((current_sim_status.roll_degrees * deg_to_rad).to_le_bytes());
            cmd.extend((current_sim_status.pitch_degrees * deg_to_rad).to_le_bytes());
            cmd.extend((current_sim_status.yaw_degrees * deg_to_rad).to_le_bytes());
            cmd.push(0u8); // Padding added to match the size of the real simulator update
            cmd.push(0u8);
            cmd.push(0u8);
            cmd.push(0u8);
            cmd.push(0u8);
            cmd.push(0u8);

            //cmd.push(10u8); //The '\n' char in UTF-8

            socket_hololens.send(&cmd)?;
            socket_unity.send(&cmd)?;
            socket_python.send(&cmd)?;

            print_preamble(&current_sim_status)?;
        }
    }

    crossterm::terminal::disable_raw_mode()?;
    Ok(())
}