/*********************************************************
 *     Professor Layton and the Curious Village tool     *
 *                  Original author ssh                  *
 *   (https://zenhax.com/viewtopic.php?p=41170#p41170)   *
 *         C# version tool made by Sudakov Pavel         *
 *********************************************************/
using System;
using System.Text;
using System.IO;

namespace PLATCV_tool
{
    public class WorkWithFiles
    {
        struct Table
        {
            public int f_offset; //File offset
            public int f_size; //File size
            public int t_name_off; //File name offset
            public string file_name; //File name
        };


        //Crypt function (for decrypt and encrypt files)
        private static void Crypt(uint offset, ref byte[] buffer, int size)
        {
            offset += 0x45243;

            for (int i = 0; i < size; i++)
            {
                offset *= 0x41C64E6D;
                offset += 0x3039;
                offset &= 0xFFFFFFFF;
                buffer[i] ^= (byte)(offset >> 0x18);
            }
        }

        //Function for remove file
        private static string GetDirName(string path)
        {
            int index = path.Length - 1;

            string result = null;

            while (true)
            {
                if (path[index] == '/' || path[index] == '\\')
                {
                    result = path.Substring(0, index);
                    break;
                }
                index--;
                if (index <= 0) break;
            }

            return result;
        }

        //Function for import files
        public static void ImportFiles(string InputFolder, string OutputFile)
        {
            DirectoryInfo di = new DirectoryInfo(InputFolder);
            FileInfo[] fi = di.GetFiles("*", SearchOption.AllDirectories); //Get files

            if (fi.Length > 0)
            {
                Array.Sort(fi, (fi1, fi2) => StringComparer.OrdinalIgnoreCase.Compare(fi1.FullName, fi2.FullName));
                Table[] tables = new Table[fi.Length];
                int offset = 0x14;

                int a_size = 0x14;
                a_size += (tables.Length * 12) + 4; //Учитываю размер таблицы
                int name_off = (tables.Length * 12) + 4;
                int f_count = fi.Length;
                int name_size = 0;
                int table_size = (tables.Length * 12) + 4;

                Console.WriteLine("Collecting data offset");

                byte[] tmp;

                byte[] table1, table2;
                table1 = new byte[4 + (12 * tables.Length)];
                tmp = BitConverter.GetBytes(f_count);
                Array.Copy(tmp, 0, table1, 0, tmp.Length);
                int t_off = 4;

                //If user forgot enter last / in folder
                bool CheckSlash = InputFolder.EndsWith("/", StringComparison.CurrentCulture) || InputFolder.EndsWith("\\", StringComparison.CurrentCulture);

                if (!CheckSlash) InputFolder += "/"; //Add if doesn't exist

                MemoryStream ms = new MemoryStream();

                for (int i = 0; i < fi.Length; i++)
                {
                    tables[i].file_name = fi[i].FullName.Remove(0, InputFolder.Length) + "\0";
                    tables[i].f_offset = offset;
                    tables[i].f_size = (int)fi[i].Length;
                    tables[i].t_name_off = name_off;
                    tmp = System.Text.Encoding.ASCII.GetBytes(tables[i].file_name);

                    ms.Write(tmp, 0, tmp.Length);

                    name_off += tmp.Length;
                    name_size += tmp.Length;

                    tmp = BitConverter.GetBytes(tables[i].t_name_off);
                    Array.Copy(tmp, 0, table1, t_off, tmp.Length);
                    t_off += 4;

                    tmp = BitConverter.GetBytes(tables[i].f_offset);
                    Array.Copy(tmp, 0, table1, t_off, tmp.Length);
                    t_off += 4;

                    tmp = BitConverter.GetBytes(tables[i].f_size);
                    Array.Copy(tmp, 0, table1, t_off, tmp.Length);
                    t_off += 4;

                    tmp = null;
                    offset += tables[i].f_size;
                }

                table2 = ms.ToArray();
                ms.Close();

                table_size += name_size;
                a_size = offset + table_size;

                byte[] data = new byte[0xffff];
                byte[] header = { 0x41, 0x52, 0x43, 0x31 };
                Array.Copy(header, 0, data, 0, header.Length);
                tmp = BitConverter.GetBytes(a_size);
                Array.Copy(tmp, 0, data, 4, tmp.Length);

                tmp = BitConverter.GetBytes(offset);
                Array.Copy(tmp, 0, data, 8, tmp.Length);

                tmp = BitConverter.GetBytes(table_size);
                Array.Copy(tmp, 0, data, 12, tmp.Length);

                Crypt(0, ref data, data.Length);

                if (File.Exists(OutputFile)) File.Delete(OutputFile);
                FileStream fs = new FileStream(OutputFile, FileMode.CreateNew);
                BinaryWriter bw = new BinaryWriter(fs);
                tmp = new byte[0x14];
                Array.Copy(data, 0, tmp, 0, tmp.Length);
                bw.Write(tmp);
                offset = 0x14;

                for (int i = 0; i < fi.Length; i++)
                {
                    tmp = File.ReadAllBytes(fi[i].FullName);
                    //Don't encrypt mp4 files!
                    if (!(fi[i].Name.Contains(".mp4") || fi[i].Name.Contains(".MP4"))) Crypt((uint)offset, ref tmp, tmp.Length);
                    bw.Write(tmp);
                    offset += tmp.Length;
                    tmp = null;

                    Console.WriteLine("{0:X8}\t{1}\t{2}", tables[i].f_offset, tables[i].f_size, fi[i].FullName.Remove(0, InputFolder.Length));
                }

                tmp = new byte[table1.Length + table2.Length];
                Array.Copy(table1, 0, tmp, 0, table1.Length);
                Array.Copy(table2, 0, tmp, table1.Length, table2.Length);

                Crypt((uint)offset, ref tmp, tmp.Length);
                bw.Write(tmp);
                bw.Close();
                fs.Close();
            }
        }

        public static void ExportFiles(string InputFile, string OutputFolder)
        {
            uint offset = 0;
            int size = 0x14;
            //int m = -1;
            byte[] header = new byte[4];
            byte[] data = new byte[size];


            FileStream fs = new FileStream(InputFile, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            data = br.ReadBytes((int)size);
            Crypt(offset, ref data, size);

            Array.Copy(data, 0, header, 0, header.Length);

            if (BitConverter.ToInt32(header, 0) == 0x31435241)
            {
                try
                {
                    size = 0xffff;
                    offset = 0;
                    byte[] tmp;
                    int t_offset; //Смещение к таблице
                    int t_size; //Размер таблицы
                    int f_count = -1; //Количество файлов
                    int a_size = -1; //Размер архива

                    br.BaseStream.Seek(0, SeekOrigin.Begin);
                    data = br.ReadBytes(size);
                    Crypt(offset, ref data, size);
                    tmp = new byte[4];
                    Array.Copy(data, 4, tmp, 0, tmp.Length);
                    a_size = BitConverter.ToInt32(tmp, 0);
                    tmp = new byte[4];
                    Array.Copy(data, 8, tmp, 0, tmp.Length);
                    t_offset = BitConverter.ToInt32(tmp, 0);
                    tmp = new byte[4];
                    Array.Copy(data, 12, tmp, 0, tmp.Length);
                    t_size = BitConverter.ToInt32(tmp, 0);

                    br.BaseStream.Seek(t_offset, SeekOrigin.Begin);

                    data = br.ReadBytes(t_size);
                    Crypt((uint)t_offset, ref data, t_size);

                    tmp = new byte[4];
                    Array.Copy(data, 0, tmp, 0, tmp.Length);
                    f_count = BitConverter.ToInt32(tmp, 0);
                    Table[] table_data = new Table[f_count];

                    offset = 4;

                    for (int i = 0; i < f_count; i++)
                    {
                        tmp = new byte[4];
                        Array.Copy(data, offset, tmp, 0, tmp.Length);
                        table_data[i].t_name_off = BitConverter.ToInt32(tmp, 0);
                        offset += 4;

                        tmp = new byte[4];
                        Array.Copy(data, offset, tmp, 0, tmp.Length);
                        table_data[i].f_offset = BitConverter.ToInt32(tmp, 0);
                        offset += 4;

                        tmp = new byte[4];
                        Array.Copy(data, offset, tmp, 0, tmp.Length);
                        table_data[i].f_size = BitConverter.ToInt32(tmp, 0);
                        offset += 4;

                        int ch_off = 0;

                        MemoryStream ms = new MemoryStream(data);
                        ms.Seek(table_data[i].t_name_off, SeekOrigin.Begin);

                        while (true)
                        {
                            tmp = new byte[1];
                            ms.Read(tmp, 0, tmp.Length);

                            if (tmp[0] == '\0') break;
                            ch_off++;
                        }
                        ms.Close();

                        tmp = new byte[ch_off];
                        Array.Copy(data, table_data[i].t_name_off, tmp, 0, tmp.Length);
                        table_data[i].file_name = Encoding.ASCII.GetString(tmp);

                        //If file is video (mp4 file) don't decrypt it!
                        bool decrypt = table_data[i].file_name.Contains(".mp4") || table_data[i].file_name.Contains(".MP4");

                        if (GetDirName(table_data[i].file_name) != null
                        && !Directory.Exists(OutputFolder + "/" + GetDirName(table_data[i].file_name))) Directory.CreateDirectory(OutputFolder + "/" + GetDirName(table_data[i].file_name));

                        if (File.Exists(OutputFolder + "/" + table_data[i].file_name)) File.Delete(OutputFolder + "/" + table_data[i].file_name);

                        br.BaseStream.Seek(table_data[i].f_offset, SeekOrigin.Begin);
                        tmp = br.ReadBytes(table_data[i].f_size);

                        if (decrypt) Crypt((uint)table_data[i].f_offset, ref tmp, table_data[i].f_size);

                        Crypt((uint)table_data[i].f_offset, ref tmp, table_data[i].f_size);

                        FileStream new_fs = new FileStream(OutputFolder + "/" + table_data[i].file_name, FileMode.CreateNew);
                        new_fs.Write(tmp, 0, tmp.Length);
                        new_fs.Close();

                        Console.WriteLine("{0:X8}\t{1}\t{2}", table_data[i].f_offset, table_data[i].f_size, table_data[i].file_name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Something wrong. Error message:\n" + ex.Message);
                }
            }
            else Console.WriteLine("This is not Professor Layton's file!");

            br.Close();
            fs.Close();
        }


        public static void GetTable(string InputFile, string OutpuFile)
        {
            uint offset = 0;
            int size = 0x14;
            //int m = -1;
            byte[] header = new byte[4];
            byte[] data = new byte[size];


            FileStream fs = new FileStream(InputFile, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            data = br.ReadBytes((int)size);
            Crypt(offset, ref data, size);

            Array.Copy(data, 0, header, 0, header.Length);

            if (BitConverter.ToInt32(header, 0) == 0x31435241)
            {
                try
                {
                    size = 0xffff;
                    offset = 0;
                    byte[] tmp;
                    int t_offset; //Смещение к таблице
                    int t_size; //Размер таблицы
                    int f_count = -1; //Количество файлов
                    int a_size = -1; //Размер архива

                    br.BaseStream.Seek(0, SeekOrigin.Begin);
                    data = br.ReadBytes(size);
                    Crypt(offset, ref data, size);
                    tmp = new byte[4];
                    Array.Copy(data, 4, tmp, 0, tmp.Length);
                    a_size = BitConverter.ToInt32(tmp, 0);
                    tmp = new byte[4];
                    Array.Copy(data, 8, tmp, 0, tmp.Length);
                    t_offset = BitConverter.ToInt32(tmp, 0);
                    tmp = new byte[4];
                    Array.Copy(data, 12, tmp, 0, tmp.Length);
                    t_size = BitConverter.ToInt32(tmp, 0);

                    br.BaseStream.Seek(t_offset, SeekOrigin.Begin);

                    data = br.ReadBytes(t_size);
                    Crypt((uint)t_offset, ref data, t_size);

                    tmp = new byte[4];
                    Array.Copy(data, 0, tmp, 0, tmp.Length);
                    f_count = BitConverter.ToInt32(tmp, 0);
                    Table[] table_data = new Table[f_count];

                    offset = 4;

                    string[] list = new string[f_count];

                    for (int i = 0; i < f_count; i++)
                    {
                        tmp = new byte[4];
                        Array.Copy(data, offset, tmp, 0, tmp.Length);
                        table_data[i].t_name_off = BitConverter.ToInt32(tmp, 0);
                        offset += 4;

                        tmp = new byte[4];
                        Array.Copy(data, offset, tmp, 0, tmp.Length);
                        table_data[i].f_offset = BitConverter.ToInt32(tmp, 0);
                        offset += 4;

                        tmp = new byte[4];
                        Array.Copy(data, offset, tmp, 0, tmp.Length);
                        table_data[i].f_size = BitConverter.ToInt32(tmp, 0);
                        offset += 4;

                        int ch_off = 0;

                        MemoryStream ms = new MemoryStream(data);
                        ms.Seek(table_data[i].t_name_off, SeekOrigin.Begin);

                        while (true)
                        {
                            tmp = new byte[1];
                            ms.Read(tmp, 0, tmp.Length);

                            if (tmp[0] == '\0') break;
                            ch_off++;
                        }
                        ms.Close();

                        tmp = new byte[ch_off];
                        Array.Copy(data, table_data[i].t_name_off, tmp, 0, tmp.Length);
                        table_data[i].file_name = Encoding.ASCII.GetString(tmp);

                        list[i] = table_data[i].f_offset.ToString("X8") + "\t" + table_data[i].f_size + "\t" + table_data[i].file_name;

                        Console.WriteLine("{0:X8}\t{1}\t{2}", table_data[i].f_offset, table_data[i].f_size, table_data[i].file_name);
                    }

                    File.WriteAllLines(OutpuFile, list);
                    Console.WriteLine("Log-file has created");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Something wrong. Error message:\n" + ex.Message);
                }
            }
            else Console.WriteLine("This is not Professor Layton's file!");

            br.Close();
            fs.Close();
        }
    }
}
