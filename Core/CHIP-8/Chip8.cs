using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Emulation;
namespace Emulators.EmuChip8
{
    public struct CPU
    {
        public byte[] memory;
        public byte[] registers;

        public ushort[] stack;

        public byte sp;
        public byte[] key;
        public ushort pc;
        public ushort I;

        //Gfx
        public byte[] gfx;

        //Timers
        public byte delay_timer;
        public byte sound_timer;
        public CPU()
        {
            memory = new byte[4096];
            registers = new byte[16];
            stack = new ushort[16];
            key = new byte[16];
            sound_timer = 0;
            delay_timer = 0;
            sp = 0;
            pc = 0x200;
            I = 0;
            gfx = new byte[64 * 32];
        }
        //Tostring operator
        public override string ToString()
        {
            return "PC: " + pc + " I: " + I + " SP: " + sp + " DT: " + delay_timer + " ST: " + sound_timer;
        }
    }



    public class Chip8 : IEmulator
    {
        public CPU cpu;
        Texture2D displayTexture;
        // Chip8 display
        private bool draw_flag = true;
        // Chip8 fontset
        private byte[] fontset = new byte[80]{
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        //Create static functions for RNG
        private static Random random = new Random();
        private static byte random_byte()
        {
            return (byte)random.Next(0, 255);
        }

        public static bool random_boolean()
        {
            return random.Next() > (Int32.MaxValue / 2);
            // Next() returns an int in the range [0..Int32.MaxValue]
        }

        public Chip8()
        {
            Initialize();
        }

        public void Initialize()
        {
            //Initialize CPU
            cpu = new CPU();
            for (int i = 0; i < 80; i++)
            {
                cpu.memory[i] = fontset[i];
            }
            //Initialize display
            displayTexture = new Texture2D(GameClass._graphics.GraphicsDevice, 64, 32);

            GameClass._graphics.PreferredBackBufferWidth = 640;  // set this value to the desired width of your window
            GameClass._graphics.PreferredBackBufferHeight = 320;   // set this value to the desired height of your window
            GameClass._graphics.ApplyChanges();

            //Load rom from content/ROM
            load_rom("Core/CHIP-8/Content/ROM/pong.ch8");
        }

        public void load_rom(string filename)
        {
            //Load rom into memory
            byte[] bytes = File.ReadAllBytes(filename);
            Array.Copy(bytes, 0, cpu.memory, 0x200, bytes.Length);
        }

        public void step()
        {
            //Fetch, decode and execute instruction at PC.

            ushort opcode = (ushort)(cpu.memory[cpu.pc] << 8 | cpu.memory[cpu.pc + 1]);
            byte x = (byte)((opcode & 0x0F00) >> 8);
            byte y = (byte)((opcode & 0x00F0) >> 4);
            byte n = (byte)(opcode & 0x000F);
            byte kk = (byte)(opcode & 0x00FF);
            ushort nnn = (ushort)(opcode & 0x0FFF);

            switch (opcode & 0xF000)
            {
                case 0x0000:
                    switch (opcode & 0x000F)
                    {
                        case 0x0000:
                            // 00E0: Clears the screen
                            for (int i = 0; i < 64 * 32; i++)
                            {
                                cpu.gfx[i] = 0;
                            }
                            cpu.pc+=2;
                            draw_flag = true;
                            break;
                        case 0x000E:
                            // 00EE: Returns from subroutine
                            cpu.pc = cpu.stack[--cpu.sp];
                            cpu.pc+=2;
                            break;
                        default:
                            throw new Exception("Illegal/Unknown Opcode: 0x" + opcode.ToString("X4"));
                    }
                    break;
                
                case 0x1000:
                    // 1nnn: Jumps to address nnn
                    cpu.pc = nnn;
                    break;
                case 0x2000:
                    // 2nnn: Calls subroutine at nnn
                    cpu.stack[cpu.sp] = cpu.pc;
                    ++cpu.sp;
                    cpu.pc = nnn;
                    break;
                case 0x3000:
                    // 3xkk: Skips the next instruction if Vx = kk
                    if (cpu.registers[x] == kk)
                    {
                        cpu.pc += 4;
                    }
                    else{
                        cpu.pc+=2;
                    }
                    break;
                case 0x4000:
                    // 4xkk: Skips the next instruction if Vx != kk
                    if (cpu.registers[x] != (opcode & 0x00FF))
                    {
                        cpu.pc += 4;
                    }
                    else{
                        cpu.pc+=2;
                    }
                    break;
                case 0x5000:
                    // 5xy0: Skips the next instruction if Vx = Vy
                    if (cpu.registers[x] == cpu.registers[y])
                    {
                        cpu.pc += 4;
                    }
                    else{
                        cpu.pc+=2;
                    }
                    break;
                case 0x6000:
                    // 6xkk: Sets Vx = kk
                    cpu.registers[x] = kk;
                    cpu.pc+=2;
                    break;
                case 0x7000:
                    cpu.registers[x] = (byte)(cpu.registers[x] + kk);
                    cpu.pc+=2;
                    break;
                //8xy0
                case 0x8000:
                    switch (n)
                    {
                        case 0x0000:
                            // 8xy0: Sets Vx = Vy
                            cpu.registers[x] = cpu.registers[y];
                            cpu.pc+=2;
                            break;
                        case 0x0001:
                            // 8xy1: Sets Vx = Vx | Vy
                            cpu.registers[x] = (byte)(cpu.registers[x] | cpu.registers[y]);
                            cpu.pc+=2;
                            break;
                        case 0x0002:
                            // 8xy2: Sets Vx = Vx & Vy
                            cpu.registers[x] = (byte)(cpu.registers[x] & cpu.registers[y]);
                            cpu.pc+=2;
                            break;
                        case 0x0003:
                            // 8xy3: Sets Vx = Vx ^ Vy
                            cpu.registers[x] = (byte)(cpu.registers[x] ^ cpu.registers[y]);
                            cpu.pc+=2;
                            break;
                        case 0x0004:
                            // 8xy4: Adds Vy to Vx. VF is set to 1 if there's a carry, 0 if there isn't
                            if (cpu.registers[x] + cpu.registers[y] > 255)
                            {
                                cpu.registers[0xF] = 1;
                            }
                            else
                            {
                                cpu.registers[0xF] = 0;
                            }
                            cpu.registers[x] += cpu.registers[y];
                            cpu.pc+=2;
                            break;
                        case 0x0005:
                            // 8xy5: Vx -= Vy. VF is set to 0 if there's a borrow, 1 if there isn't
                            if (cpu.registers[x] > cpu.registers[y])
                            {
                                cpu.registers[0xF] = 1;
                            }
                            else
                            {
                                cpu.registers[0xF] = 0;
                            }
                            cpu.registers[x] -= cpu.registers[y];
                            cpu.pc+=2;
                            break;
                        case 0x0006:
                            // 8xy6: Shifts Vx right by one. VF is set to the value of the least significant bit of Vx before the shift
                            cpu.registers[0xF] = (byte)(cpu.registers[x] & 0x1);
                            cpu.registers[x] <<= 1;
                            cpu.pc+=2;
                            break;
                        case 0x0007:
                            cpu.registers[0xF]=cpu.registers[y]>cpu.registers[x] ? (byte)1 : (byte)0;                           
                            cpu.registers[x]-=cpu.registers[y];
                            cpu.pc+=2;
                            break;
                        case 0x000E:
                            // 8xyE:If the most-significant bit of Vx is 1, then VF is set to 1, otherwise to 0. Then Vx is multiplied by 2.
                            cpu.registers[0xF] = (byte)(cpu.registers[x] >> 7)==1 ? (byte)1 : (byte)0;
                            cpu.registers[x] *= 2;
                            cpu.pc+=2;
                            break;
                        default:
                            throw new Exception("Illegal/Unknown Opcode: 0x" + opcode.ToString("X4"));
                    }
                    break;
                case 0x9000:
                    // 9xy0: Skips the next instruction if Vx != Vy
                    if (cpu.registers[x] != cpu.registers[y])
                    {
                        cpu.pc+=4;
                    }
                    else{
                        cpu.pc+=2;
                    }
                    break;

                case 0xA000:
                    // Annn: Sets I to the address nnn
                    cpu.I = (ushort)(opcode & 0x0FFF);
                    cpu.pc+=2;
                    break;

                case 0xB000:
                    cpu.pc = (ushort)(nnn + cpu.registers[0]);
                    break;
                case 0xC000:
                    byte rand_byte = random_byte();
                    cpu.registers[x] = (byte)(rand_byte & kk);
                    cpu.pc+=2;
                    break;
                case 0xD000:
                    //Draws a sprite at coordinate (Vx, Vy) that has a width of 8 pixels and a height of N pixels.
                    cpu.registers[0xF] = 0;
                    for (int yline = 0; yline < n; yline++)
                    {
                        byte sprite_line = cpu.memory[cpu.I + (byte)yline];
                        for (int xline = 0; xline < 8; xline++)
                        {
                            if ((sprite_line & (0x80 >> xline)) != 0)
                            {
                                if (cpu.gfx[(cpu.registers[x] + xline + ((cpu.registers[y] + yline) * 64))] == 1)
                                {
                                    cpu.registers[0xF] = 1;
                                }
                                cpu.gfx[(cpu.registers[x] + xline + ((cpu.registers[y] + yline) * 64))] ^= 1;
                            }
                        }
                    }
                    draw_flag = true;
                    cpu.pc+=2;
                    break;
                case 0xF000:
                    switch (kk)
                    {
                        case 0x0007:
                            // Fx07: Sets Vx = delay timer value
                            cpu.registers[x] = cpu.delay_timer;
                            cpu.pc+=2;
                            break;
                        case 0x000A:
                            // Fx0A: A key press is awaited, and then stored in Vx
                            bool key_pressed = false;
                            for (int i = 0; i < 16; i++)
                            {
                                if (cpu.key[i] != 0)
                                {
                                    cpu.registers[x] = (byte)i;
                                    key_pressed = true;
                                    break;
                                }
                            }
                            if (!key_pressed)
                            {
                                return;
                            }
                            cpu.pc+=2;
                            break;
                        case 0x0015:
                            // Fx15: Sets the delay timer to Vx
                            cpu.delay_timer = cpu.registers[x];
                            cpu.pc+=2;
                            break;
                        case 0x0018:
                            // Fx18: Sets the sound timer to Vx
                            cpu.sound_timer = cpu.registers[x];
                            cpu.pc+=2;
                            break;
                        case 0x001E:
                            // Fx1E: Adds Vx to I
                            if (cpu.I + cpu.registers[x] > 0xFFF)
                            {
                                cpu.registers[0xF] = 1;
                            }
                            else
                            {
                                cpu.registers[0xF] = 0;
                            }
                            cpu.I += cpu.registers[x];
                            cpu.pc+=2;
                            break;
                        case 0x0029:
                            // Fx29: Sets I to the location of the sprite for the character in Vx. Characters 0-F (in hexadecimal) are represented by a 4x5 font
                            cpu.I = (ushort)(cpu.registers[x] * 0x5);
                            cpu.pc+=2;
                            break;
                        case 0x0033:
                            // 0xFX33: Stores the binary-coded decimal representation of Vx, with the most significant of three digits at the address in I, the middle digit at I plus 1, and the least significant digit at I plus 2.
                            cpu.memory[cpu.I] = (byte)(cpu.registers[x] / 100);
                            cpu.memory[cpu.I + 1] = (byte)((cpu.registers[x] / 10) % 10);
                            cpu.memory[cpu.I + 2] = (byte)(cpu.registers[x] % 10);
                            cpu.pc+=2;
                            break;
                        case 0x0055:
                            // Fx55: Stores V0 to Vx in memory starting at address I
                            for (int i = 0; i <= x; i++)
                            {
                                cpu.memory[cpu.I + i] = cpu.registers[i];
                            }
                            cpu.pc+=2;
                            break;
                        case 0x0065:
                            // Fx65: Fills V0 to Vx with values from memory starting at address I
                            for (int i = 0; i <= x; i++)
                            {
                                cpu.registers[i] = cpu.memory[cpu.I + i];
                            }
                            cpu.pc+=2;
                            break;
                        default:
                            throw new Exception("Illegal/Unknown Opcode: 0x" + opcode.ToString("X4"));

                    }
                    break;
                case 0xE000:
                    switch (kk)
                    {
                        case 0x009E:
                            //ExA1: Skips the next instruction if the key stored in Vx is pressed
                            if (cpu.key[cpu.registers[x]] != 0)
                            {
                                cpu.pc += 2;
                            }
                            break;
                        case 0x00A1:
                            //ExA1: Skips the next instruction if the key stored in Vx is not pressed
                            if (cpu.key[cpu.registers[x]] == 0)
                            {
                                cpu.pc += 2;
                            }
                            break;
                    }
                    break;
                default:
                    throw new Exception("Illegal/Unknown Opcode: 0x" + opcode.ToString("X4"));
            }
            if (cpu.delay_timer > 0)
            {
                cpu.delay_timer--;
            }
            if (cpu.sound_timer > 0)
            {
                if (cpu.sound_timer == 1)
                {
                    Console.WriteLine("BEEP!");
                }
                cpu.sound_timer--;
            }
        }

        int last_update = 0;
        //Implement interface
        public void Update(GameTime gameTime)
        {
            step();
            if (draw_flag)
            {
                draw_flag = false;

                //Set displayTexture data
                Color[] data = new Color[64 * 32];
                for (int y = 0; y < 32; ++y)
                {
                    for (int x = 0; x < 64; ++x)
                    {
                        if (cpu.gfx[(y * 64) + x] == 0)
                        {
                            data[(y * 64) + x] = Color.Black;
                        }
                        else
                        {
                            data[(y * 64) + x] = Color.White;
                        }
                    }
                }
                displayTexture.SetData(data);
            }
        }

        public void LoadContent()
        {
        }

        public void Draw()
        {
            var device = GameClass._graphics.GraphicsDevice;

            //Draw texture and scale it up to window size
            GameClass._spriteBatch.Draw(displayTexture, new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height), Color.White);
            //Set keyboard input
            cpu.key[0x1] = (byte) (Keyboard.GetState().IsKeyDown(Keys.D1) ? 1 : 0);
            cpu.key[0x2] = (byte)(Keyboard.GetState().IsKeyDown(Keys.D2) ? 1 : 0);
            cpu.key[0x3] = (byte)(Keyboard.GetState().IsKeyDown(Keys.D3) ? 1 : 0);
            cpu.key[0xC] = (byte)(Keyboard.GetState().IsKeyDown(Keys.D4) ? 1 : 0);

            cpu.key[0x4] = (byte)(Keyboard.GetState().IsKeyDown(Keys.Q) ? 1 : 0);
            cpu.key[0x5] = (byte)(Keyboard.GetState().IsKeyDown(Keys.W) ? 1 : 0);
            cpu.key[0x6] = (byte)(Keyboard.GetState().IsKeyDown(Keys.E) ? 1 : 0);
            cpu.key[0xD] = (byte)(Keyboard.GetState().IsKeyDown(Keys.R) ? 1 : 0);

            cpu.key[0x7] = (byte)(Keyboard.GetState().IsKeyDown(Keys.A) ? 1 : 0);
            cpu.key[0x8] = (byte)(Keyboard.GetState().IsKeyDown(Keys.S) ? 1 : 0);
            cpu.key[0x9] = (byte)(Keyboard.GetState().IsKeyDown(Keys.D) ? 1 : 0);
            cpu.key[0xE] = (byte)(Keyboard.GetState().IsKeyDown(Keys.F) ? 1 : 0);
            
            cpu.key[0xA] = (byte)(Keyboard.GetState().IsKeyDown(Keys.Z) ? 1 : 0);
            cpu.key[0x0] = (byte)(Keyboard.GetState().IsKeyDown(Keys.X) ? 1 : 0);
            cpu.key[0xB] = (byte)(Keyboard.GetState().IsKeyDown(Keys.C) ? 1 : 0);
            cpu.key[0xF] = (byte)(Keyboard.GetState().IsKeyDown(Keys.V) ? 1 : 0);        
        }
    }
}