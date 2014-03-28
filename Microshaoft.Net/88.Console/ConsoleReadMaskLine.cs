namespace Microshaoft
{
    using System;
    public class Class1
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter password:");
            string password = ConsoleReadMaskLine('*', true);
            Console.WriteLine("\n" + password + "]");
            password = ConsoleReadMaskLine('%', false);
            Console.WriteLine("\n" + password + "]");
        }

        public static string ConsoleReadMaskLine
            (
                char PasswordChar
                , bool WithMask
            )
        {
            string password = "";
            ConsoleKey ck;
            string s = @"~!@#$%&*()_+`1234567890-="; //可输入字符
            s += @"QWERTYUIOP{}|qwertyuiop[]\";
            s += "ASDFGHJKL:\"asdfghjkl;'";
            s += "ZXCVBNM<>?zxcvbnm,./ ";

            do
            {
                ConsoleKeyInfo cki = Console.ReadKey(true);
                char c = cki.KeyChar;
                ck = cki.Key;
                int p = Console.CursorLeft;
                if (ck == ConsoleKey.Backspace)
                {
                    string left = "";
                    if (p > 0)
                    {
                        left = password.Substring(0, p - 1);
                    }
                    string right = password.Substring(p);
                    password = left + right;
                    Console.Write(c);

                    string output = right;
                    if (WithMask)
                    {
                        output = GetPasswordChars(right, PasswordChar);
                    }

                    output += "\0";
                    Console.Write(output);
                    if (p > 0)
                    {
                        p--;
                    }
                }
                else if (ck == ConsoleKey.Delete)
                {
                    string left = "";
                    if (p > 0)
                    {
                        left = password.Substring(0, p);
                    }
                    string right = "";
                    if (p < password.Length)
                    {
                        right = password.Substring(p + 1);
                    }
                    password = left + right;
                    //Console.Write(right + " ");

                    string output = right;

                    if (WithMask)
                    {
                        output = GetPasswordChars(right, PasswordChar);
                    }
                    output += "\0";

                    Console.Write(output);
                }
                else
                {
                    if (s.IndexOf(c) >= 0)
                    {
                        string left = password.Substring(0, p);
                        string right = password.Substring(p);
                        password = left + c + right;

                        string output = c + right;

                        if (WithMask)
                        {
                            output = GetPasswordChars(c + right, PasswordChar);
                        }
                        Console.Write(output);

                        p++;
                    }
                    else
                    {
                        switch (ck)
                        {
                            case ConsoleKey.LeftArrow:
                                if (p > 0)
                                {
                                    p--;
                                }
                                break;
                            case ConsoleKey.RightArrow:
                                if (p < password.Length)
                                {
                                    p++;
                                }
                                break;
                            case ConsoleKey.Home:
                                p = 0;
                                break;
                            case ConsoleKey.End:
                                p = password.Length;
                                break;
                            default:
                                Console.Beep();
                                break;
                        }
                    }
                }
                Console.CursorLeft = p;
            } while (ck != ConsoleKey.Enter);
            return password;
        }
        private static string GetPasswordChars(string s, char c)
        {
            string passwordChars = "";
            for (int i = 0; i < s.Length; i++)
            {
                passwordChars += c;
            }
            return passwordChars;
        }
    }
}