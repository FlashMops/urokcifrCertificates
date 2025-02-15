using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class UrokCifriCertificate
{
    public static async Task Main()
    {
        string configUrl = "https://raw.githubusercontent.com/FlashMops/urokcifrCertificates/refs/heads/main/config/config.json";

        //Console.WriteLine("Загружаем конфигурацию...");
        Config config = await LoadConfigAsync(configUrl);

        if (config?.certificates == null || config.certificates.Count == 0)
        {
            Console.WriteLine("Конфигурация пуста или не загружена.");
            return;
        }

        DisplayCertificates(config);
        Certificate selectedCertificate = SelectCertificate(config);
        Random random = new Random();

        Console.Write("Введите имя и фамилию или 'list' для загрузки из list.txt: ");
        string input = Console.ReadLine();

        List<string> names = new List<string>();

        if (input.ToLower() == "list")
        {
            Console.WriteLine("Читаем имена из list.txt...");
            names.AddRange(File.ReadAllLines("list.txt"));
        }
        else
        {
            names.Add(input);
        }

        foreach (string nameSurname in names)
        {
            if (string.IsNullOrWhiteSpace(nameSurname)) continue;

            string templateUrl = selectedCertificate.direct_links[random.Next(selectedCertificate.direct_links.Count)];
            string templatePath = await DownloadTemplateAsync(templateUrl);

            string sanitizedFileName = nameSurname.Replace(" ", "").Replace(".", "").Replace(",", "");
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{sanitizedFileName}.jpg");
            string code = GenerateRandomCode();

            GenerateCertificate(templatePath, outputPath, nameSurname, code, selectedCertificate.namePos, selectedCertificate.codePos);
            Console.WriteLine($"Сертификат для {nameSurname} сохранен как {sanitizedFileName}.jpg");
        }
    }

    static async Task<Config> LoadConfigAsync(string url)
    {
        string localConfigPath = "config.json";

        if (File.Exists(localConfigPath))
        {
            Console.WriteLine("Загружаем конфигурацию из локального файла...");
            string json = File.ReadAllText(localConfigPath);
            return JsonConvert.DeserializeObject<Config>(json);
        }
        else
        {
            Console.WriteLine("Локальный config.json не найден, загружаем с GitHub...");
            using (HttpClient client = new HttpClient())
            {
                string json = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<Config>(json);
            }
        }
    }

    static void DisplayCertificates(Config config)
    {
        Console.WriteLine("Доступные сертификаты:");
        for (int i = 0; i < config.certificates.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {config.certificates[i].name} (введите {i + 1} для выбора)");
        }
    }

    static Certificate SelectCertificate(Config config)
    {
        int choice;
        do
        {
            Console.Write("Введите номер сертификата для выбора: ");
        } while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > config.certificates.Count);

        return config.certificates[choice - 1];
    }

    static async Task<string> DownloadTemplateAsync(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            Console.WriteLine($"Скачиваем шаблон...");
            byte[] templateData = await client.GetByteArrayAsync(url);
            string tempFilePath = Path.Combine(Directory.GetCurrentDirectory(), "template.jpg");

            File.WriteAllBytes(tempFilePath, templateData);
            return tempFilePath;
        }
    }

    public static void GenerateCertificate(string templatePath, string outputPath, string nameSurname, string code, Position namePos, Position codePos)
    {
        using (Image template = Image.FromFile(templatePath))
        using (Graphics graphics = Graphics.FromImage(template))
        {
            PrivateFontCollection fontCollection = new PrivateFontCollection();
            AddFontFromResource(fontCollection, "UrokCifriCertificate.Fonts.DejaVuSans.ttf");
            Font nameFont = new Font(fontCollection.Families[0], 26, FontStyle.Regular);
            Font codeFont = new Font(fontCollection.Families[0], 15, FontStyle.Regular);
            Brush textBrush = Brushes.Black;

            PointF namePosition = new PointF(namePos.x - graphics.MeasureString(nameSurname, nameFont).Width / 2, namePos.y);
            PointF codePosition = new PointF(codePos.x, codePos.y);

            graphics.DrawString(nameSurname, nameFont, textBrush, namePosition);
            graphics.DrawString(code, codeFont, textBrush, codePosition);

            template.Save(outputPath, ImageFormat.Jpeg);
        }
        File.Delete(templatePath);
    }

    private static void AddFontFromResource(PrivateFontCollection fontCollection, string resourceName)
    {
        using (Stream fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (fontStream == null) throw new FileNotFoundException("Не удалось найти ресурс шрифта: " + resourceName);

            byte[] fontData = new byte[fontStream.Length];
            fontStream.Read(fontData, 0, fontData.Length);
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            fontCollection.AddMemoryFont(fontPtr, fontData.Length);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
        }
    }

    public static string GenerateRandomCode()
    {
        const int length = 8;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder result = new StringBuilder(length);
        Random random = new Random();

        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }

        return result.ToString();
    }
}

public class Position
{
    public int x { get; set; }
    public int y { get; set; }
}

public class Certificate
{
    public Position namePos { get; set; }
    public Position codePos { get; set; }
    public string name { get; set; }
    public List<string> direct_links { get; set; }
}

public class Config
{
    public List<Certificate> certificates { get; set; }
}
