using System;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace PHF2BIN
{
    public partial class PHF2BIN : Form
    {
        public PHF2BIN()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PHF File|*.phf";
            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK) return;
            string fileName = openFileDialog.FileName;

            if (!File.Exists(fileName))
            {
                MessageBox.Show("File does not exist: " + fileName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] rawBinaryFile = ParseFile(File.ReadAllBytes(fileName));

            if (rawBinaryFile != null)
            {
                SaveFile(rawBinaryFile);
                return;
            }
            else
            {
                MessageBox.Show("Invalid file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        int FindBytes(byte[] rawBinary, byte[] needle, int searchLimit = int.MaxValue)
        {
            int len = needle.Length;
            int limit = rawBinary.Length - len < searchLimit ? rawBinary.Length - len : searchLimit;
            for (int i = 0; i <= limit; i++)
            {
                int k = 0;
                for (; k < len; k++)
                {
                    byte searchByte = needle[k];
                    byte binaryByte = rawBinary[i + k];
                    if (searchByte != binaryByte) break;
                }
                if (k == len) return i;
            }
            return -1;
        }

        byte[] ParseFile(byte[] bytes)
        {
            byte[] rawBinary;

            int outputSize;
            byte[] header = new byte[0x12];
            byte[] spanishOak = ASCIIEncoding.ASCII.GetBytes("SPANISHOAK");
            byte[] blackOak = ASCIIEncoding.ASCII.GetBytes("BOAK");
            if (FindBytes(bytes, spanishOak, 0x100) != -1)
            {
                header[0x10] = 0x10;
                header[0x11] = 0x60;
                outputSize = 0x100000;
            }
            else if (FindBytes(bytes, blackOak, 0x100) != -1)
            {
                header[0x10] = 0x30;
                header[0x11] = 0x60;
                outputSize = 0x170000;
            }
            else return null;

            rawBinary = new byte[outputSize];

            //Find the magic header bytes
            int offset = FindBytes(bytes, header);
            if (offset == -1) return null;

            int binIndex = 0;
            for (int phfIndex = offset; phfIndex < bytes.Length;)
            {
                //8 Byte header before we start again
                if (binIndex % 0x10000 == 0 && binIndex != 0) phfIndex += 8;
                if (binIndex >= rawBinary.Length) break;

                //32768-65536 is missing??
                if (binIndex >= 0x8000 && binIndex < 0x10000)
                {
                    rawBinary[binIndex] = 0xFF;
                    binIndex++;
                }
                else
                {
                    //Every 32 byte block we have a 6 byte header to remove
                    if (binIndex % 32 == 0 && binIndex != 0) phfIndex += 6;

                    rawBinary[binIndex] = bytes[phfIndex];
                    phfIndex++;
                    binIndex++;
                }
            }

            return rawBinary;
        }

        void SaveFile(byte [] rawBinary)
        {
            SaveFileDialog savefile = new SaveFileDialog();

            savefile.Filter = "Binary File|*.bin";

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(savefile.FileName, rawBinary);
            }
        }
    }
}
