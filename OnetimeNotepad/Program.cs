using System.Security.Cryptography;
using System.Text;

namespace OnetimeNotepad;

public static class OneTimePad
{
    private const int Base = 100;

    private static readonly string Latin = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string Cyrillic = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
    private static readonly string AllLetters = Latin + Cyrillic;

    private static readonly string?[] DigitWords =
        ["ZERO", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE"];

    private static readonly Dictionary<char, string?> Punctuation = new()
    {
        [','] = "COMMA", ['.'] = "X", ['!'] = "EXCLAMATION", ['?'] = "QUESTION",
        [';'] = "SEMICOLON", [':'] = "COLON", ['-'] = "DASH", ['—'] = "DASH",
        ['('] = "LEFTBRACKET", [')'] = "RIGHTBRACKET",
        ['['] = "LEFTSQUARE", [']'] = "RIGHTSQUARE",
        ['{'] = "LEFTCURLY", ['}'] = "RIGHTCURLY",
        ['\''] = "QUOTE", ['"'] = "QUOTE", ['…'] = "ELLIPSIS"
    };

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "XX";

        var tokens = new List<string?>();
        var words = input.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            int i = 0;
            while (i < word.Length)
            {
                char c = char.ToUpperInvariant(word[i]);

                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < word.Length && char.IsDigit(word[i])) i++;
                    string num = word.Substring(start, i - start);
                    if (num.Length == 1)
                        tokens.Add(DigitWords[num[0] - '0']);
                    else
                        tokens.AddRange(num.Select(d => DigitWords[d - '0']));
                    continue;
                }

                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < word.Length && char.IsLetter(word[i])) i++;
                    string letters = word.Substring(start, i - start).ToUpperInvariant();
                    tokens.Add(letters);
                    continue;
                }

                if (Punctuation.TryGetValue(c, out string? rep))
                {
                    tokens.Add(rep);
                }

                i++;
            }
        }

        string result = string.Join(" ", tokens).Trim();
        if (!result.EndsWith("XX")) result += " XX";
        return result;
    }

    public static int[] TextToCodes(string normalized)
    {
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var codes = new List<int>();

        for (int p = 0; p < parts.Length; p++)
        {
            foreach (char c in parts[p])
            {
                char uc = char.ToUpperInvariant(c);

                int idx = Latin.IndexOf(uc);
                if (idx >= 0)
                {
                    codes.Add(idx + 1);
                    continue;
                }

                idx = Cyrillic.IndexOf(uc);
                if (idx >= 0)
                {
                    codes.Add(26 + idx + 1);
                }
            }

            if (p < parts.Length - 1)
                codes.Add(0);
        }

        return codes.ToArray();
    }

    private static string CodesToText(int[] codes)
    {
        var sb = new StringBuilder();
        foreach (int code in codes)
        {
            if (code == 0)
            {
                sb.Append(' ');
            }
            else if (code <= 26)
            {
                sb.Append(Latin[code - 1]);
            }
            else if (code <= 59)
            {
                sb.Append(Cyrillic[code - 27]);
            }
            else
            {
                sb.Append('?');
            }
        }

        return sb.ToString();
    }

    public static int[] GenerateKey(int length)
    {
        var key = new int[length];
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        for (int i = 0; i < length; i++)
            key[i] = bytes[i] % 100;
        return key;
    }

    public static int[] Encrypt(string message, int[] key)
    {
        string norm = Normalize(message);
        var codes = TextToCodes(norm);
        if (key.Length < codes.Length) throw new ArgumentException("Ключ короче сообщения");
        var cipher = new int[codes.Length];
        for (int i = 0; i < codes.Length; i++)
            cipher[i] = (codes[i] + key[i]) % Base;
        return cipher;
    }

    public static string Decrypt(int[] cipher, int[] key)
    {
        if (key.Length < cipher.Length)
            throw new ArgumentException("Ключ короче шифра");

        var codes = new int[cipher.Length];
        for (int i = 0; i < cipher.Length; i++)
            codes[i] = (cipher[i] - key[i] + Base) % Base;

        string raw = CodesToText(codes);

        return SplitToWords(raw);
    }

    static string SplitToWords(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            if (c == ' ')
                sb.Append(' ');
            else
                sb.Append(c);
        }

        return sb.ToString().Trim();

}


    public static string Format(int[] arr) => string.Join(" ", arr.Select(x => x.ToString("D2")));
    public static int[] Parse(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
}

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("ОДНОРАЗОВЫЙ БЛОКНОТ\n");

        while (true)
        {
            Console.WriteLine("1 — Зашифровать сообщение");
            Console.WriteLine("2 — Расшифровать по ключу");
            Console.WriteLine("3 — Выход\n");

            string? choice = Console.ReadLine()?.Trim();

            if (choice == "3") break;
            if (choice == "1") EncryptMode();
            else if (choice == "2") DecryptMode();
            else Console.WriteLine("Выбери 1, 2 или 3.");

            Console.WriteLine("\nНажми Enter...");
            Console.ReadLine();
        }
    }

    public static string ReadMultiline()
    {
        var sb = new StringBuilder();
        while (true)
        {
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                break;

            sb.Append(line.Trim());
            sb.Append(' ');
        }

        return sb.ToString().Trim();
    }

    static void EncryptMode()
    {
        Console.Write("Введи сообщение: ");
        string msg = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(msg))
        {
            Console.WriteLine("Сообщение пустое.");
            return;
        }

        string norm = OneTimePad.Normalize(msg);
        Console.WriteLine($"\nНормализовано:\n{norm}\n");

        var codes = OneTimePad.TextToCodes(norm);
        var key = OneTimePad.GenerateKey(codes.Length);
        var cipher = OneTimePad.Encrypt(msg, key);

        Console.WriteLine("КЛЮЧ (Сохрани его в надёжном месте!):");
        Console.WriteLine(OneTimePad.Format(key));
        Console.WriteLine("\nШИФРОТЕКСТ (передавай открыто):");
        Console.WriteLine(OneTimePad.Format(cipher));
    }

    static void DecryptMode()
    {
        Console.Write("Введи шифротекст (числа через пробел): ");
        string cipherText = ReadMultiline();
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            Console.WriteLine("Ты забыл ввести шифротекст.");
            return;
        }

        int[] cipher;
        try
        {
            cipher = OneTimePad.Parse(cipherText);
        }
        catch
        {
            Console.WriteLine("Шифротекст введён неправильно.");
            return;
        }

        Console.Write("Введи ключ (те же числа): ");
        string keyText = ReadMultiline();

        if (string.IsNullOrWhiteSpace(keyText))
        {
            Console.WriteLine("Ключ забыл?");
            return;
        }

        int[] key;
        try
        {
            key = OneTimePad.Parse(keyText);
        }
        catch
        {
            Console.WriteLine("Ключ введён криво — ты точно не шпион?");
            return;
        }

        string decrypted;
        try
        {
            decrypted = OneTimePad.Decrypt(cipher, key);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return;
        }

        Console.WriteLine($"\nРасшифровано:\n{decrypted}");
    }
}