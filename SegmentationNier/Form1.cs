using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace SegmentationNier
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Bitmap image;
        Bitmap maskImage;
        List<double[,]> spectres = new List<double[,]>();
        List<string> temp = new List<string>
        {
            "dzn",
            "m",
            "s",
            "ns",
            "te",
            "me",
            "sc",
            "pc",
            "rg",
            "bg"
        };
        string inputImage = @"input\test.bmp";
        string inputMask = @"input\test_m.bmp";
        private void button1_Click(object sender, EventArgs e)
        {
            image = new Bitmap(inputImage);
            maskImage = new Bitmap(inputMask);
            ImageProcessor one = new ImageProcessor();
            one.Accuracy = 0.7f;
            one.ImageMask = maskImage;
            one.ImageSource = image;
            Thread thread = new Thread(new ParameterizedThreadStart(one.makePoints));
            thread.Start(new MaskInfo(10, "me"));
            thread.Join();
            //one.makePoints(new MaskInfo(10, "me"));

        }
        BackgroundWorker up;
        int max;
        private void button2_Click(object sender, EventArgs e)
        {
            image = new Bitmap(inputImage);
            maskImage = new Bitmap(inputMask);
            string outPath = @"output\test\";
            ImageProcessor one = new ImageProcessor();
            for(int i = 8; i < 12; i++)
            {
                foreach(string className in temp)
                {
                    string outPath2 = outPath + className + @"\" + i.ToString() + @"\";
                    one.FragWindows(image, one.MakePoints(maskImage, i, className, 0.8f), i, outPath2);
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            string inPath = @"output\test\";
            string classImage = textBox2.Text;
            inPath += classImage + @"\";
            inPath += textBox1.Text + @"\";
            ImageProcessor imageProcessor = new ImageProcessor();
            string[] files = Directory.GetFiles(inPath);
            int cf = files.Length;
            for (int i = 0; i < cf; i++)
            {
                PathSpectreInfo pathInfo = new PathSpectreInfo();
                string inPath2 = inPath + i.ToString() + ".bmp";
                pathInfo.InPath = inPath2;
                pathInfo.OutPath = @"output\test\" + classImage + @"\spectre\" + textBox1.Text + @"\" + i.ToString();
                Thread thread = new Thread(new ParameterizedThreadStart(imageProcessor.calculateSpectre));
                thread.Start(pathInfo);

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string inPath = @"output\test\";
            
            string classImage = textBox2.Text;
            inPath += classImage + @"\spectre\" + textBox1.Text + @"\";
            string[] files = Directory.GetFiles(inPath);
            max = files.Length;
            int i = 0;
            int cf = files.Length / 2;
            while (i < cf)
            {
                string inPath2 = inPath + i.ToString() + "_M.txt";
                PathSpectreInfo info = new PathSpectreInfo();
                info.InPath = inPath2;
                info.n = Convert.ToInt32(textBox1.Text);
                Thread thread = new Thread(new ParameterizedThreadStart(ImageProcessor.ParseSpectreFromFile));
                thread.Start(info);
                i++;
            }
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(@"output\spectres_test_M_" + classImage + "_" + textBox1.Text + ".bin", FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, ImageProcessor.spectres);
            }
            ImageProcessor.spectres.Clear();
            //ImageProcessor.counter = 0;
            //ImageProcessor.ParseSpectreFromFile(@"output\1\s\spectre\10\1_M.txt", 10);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog opf = new OpenFileDialog();
            opf.Title = "Выберите файл:";
            opf.Filter = "Файл спектров: (*.bin)|*.bin";
            List<double[,]> spectres = new List<double[,]>();
            if (opf.ShowDialog() == DialogResult.OK)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream fs = new FileStream(opf.FileName, FileMode.OpenOrCreate))
                {
                    spectres = (List<double[,]>)formatter.Deserialize(fs);
                }
            }
            int[] count = new int[100];
            int x = Convert.ToInt32(textBox3.Text);
            int y = Convert.ToInt32(textBox4.Text);
            int j = 0;
            foreach(double[,] spectre in spectres)
            {
                if (spectre == null)
                {

                }
                else
                {
                    int value = (int)spectre[x, y];
                    if (value > 6)
                    {
                        count[value]++;
                    }
                    j++;
                }
            }
            Graph graph = new Graph();
            GraphPane pane = graph.zedGraphControl1.GraphPane;
            PointPairList list = new PointPairList();
            for (int i = 0; i < 100; i++)
            {
                // добавим в список точку
                list.Add(i, count[i]*110);
                
            }
            pane.Title.Text = "N = 8";
            LineItem myCurve = pane.AddCurve("x = " + x.ToString() + " y = " + y.ToString(), list, Color.Blue, SymbolType.None);
            pane.XAxis.MajorGrid.IsVisible = true;
            pane.XAxis.MajorGrid.DashOn = 10;
            pane.XAxis.MajorGrid.DashOff = 5;
            pane.YAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.DashOn = 10;
            pane.YAxis.MajorGrid.DashOff = 5;
            pane.YAxis.MinorGrid.IsVisible = true;
            pane.YAxis.MinorGrid.DashOn = 1;
            pane.YAxis.MinorGrid.DashOff = 2;
            pane.XAxis.MinorGrid.IsVisible = true;
            pane.XAxis.MinorGrid.DashOn = 1;
            pane.XAxis.MinorGrid.DashOff = 2;
            graph.zedGraphControl1.AxisChange();
            graph.zedGraphControl1.Invalidate();
            graph.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            string inPath = @"output\5516_M.txt";
            PathSpectreInfo info = new PathSpectreInfo();
            info.InPath = inPath;
            info.n = Convert.ToInt32(textBox1.Text);
            ImageProcessor.ParseSpectreFromFile(info);
            int[,] k = new int[10, 10];
            for(int i = 0; i < 10; i++)
            {
                for(int j = 0; j < 10; j++)
                {
                    k[i, j] = (int)ImageProcessor.spectres[0][i, j];
                }
            }
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(@"output\Max_Count_spectre_values_s_2_Magnitude" + ".bin", FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, k);
            }
            /*
            string inPath = @"output\1\";

            string classImage = textBox2.Text;
            inPath += classImage + @"\spectre\" + textBox1.Text + @"\";
            string[] files = Directory.GetFiles(inPath);
            max = files.Length;
            int i = 0;
            int cf = files.Length / 2;
            while (i < cf)
            {
                string inPath2 = inPath + i.ToString() + "_P.txt";
                PathSpectreInfo info = new PathSpectreInfo();
                info.InPath = inPath2;
                info.n = Convert.ToInt32(textBox1.Text);
                Thread thread = new Thread(new ParameterizedThreadStart(ImageProcessor.ParseSpectreFromFile));
                thread.Start(info);
                i++;
            }
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(@"output\spectres_1_P_" + classImage + "_" + textBox1.Text + ".bin", FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, ImageProcessor.spectres);
            }
            ImageProcessor.spectres.Clear();*/
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string inPath = @"output\";
            string[] files = Directory.GetFiles(inPath);
            string className = textBox2.Text;
            List<double[,]> spectres = new List<double[,]>();
            foreach (string fileName in files)
            {
                if (fileName.Contains("_M_" + className + "_8"))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    List<double[,]> spectres_temp = new List<double[,]>();
                    using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                    {
                        spectres_temp = (List<double[,]>)formatter.Deserialize(fs);
                        //spectres.Concat((List<double[,]>)formatter.Deserialize(fs));
                    }
                    spectres = spectres.Concat(spectres_temp).ToList();
                }
            }
            int[,] peaks = new int[8, 8];
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int[] count = new int[100]; 
                    foreach (double[,] spectre in spectres)
                    {
                        if (spectre == null)
                        {

                        }
                        else
                        {
                            int value = (int)spectre[x, y];
                            count[value]++;
                        }
                    }
                    
                    int v_index = 0;
                    bool check = false;
                    while (!check)
                    {
                        if(count[v_index] == count.Max())
                        {
                            check = true;
                        }
                        else
                        {
                            v_index++;
                        }
                    }
                    peaks[x, y] = v_index;
                }
            }
            BinaryFormatter formatter2 = new BinaryFormatter();
            using (FileStream fs = new FileStream(@"output\Max_Count_spectre_values_" + className + "_Magnitude_8" + ".bin", FileMode.OpenOrCreate))
            {
                formatter2.Serialize(fs, peaks);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string inputImage = @"input\tp.bmp";
            Bitmap image = new Bitmap(inputImage);
            pictureBox1.Image = image;
            int[,] spectreMaskS;
            int[,] spectreMaskM;
            int[,] spectreMaskME;
            int[,] spectreMaskTE;
            int[,] spectreMaskRG;
            string fileS = @"output\Max_Count_spectre_values_s_Magnitude.bin";
            string fileM = @"output\Max_Count_spectre_values_m_Magnitude.bin";
            string fileME = @"output\Max_Count_spectre_values_me_Magnitude.bin";
            string fileTE = @"output\Max_Count_spectre_values_te_Magnitude.bin";
            string fileRG = @"output\Max_Count_spectre_values_rg_Magnitude.bin";
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(fileS, FileMode.OpenOrCreate))
            {
                spectreMaskS = (int[,])formatter.Deserialize(fs);
            }
            using (FileStream fs = new FileStream(fileM, FileMode.OpenOrCreate))
            {
                spectreMaskM = (int[,])formatter.Deserialize(fs);
            }
            using (FileStream fs = new FileStream(fileME, FileMode.OpenOrCreate))
            {
                spectreMaskME = (int[,])formatter.Deserialize(fs);
            }
            using (FileStream fs = new FileStream(fileTE, FileMode.OpenOrCreate))
            {
                spectreMaskTE = (int[,])formatter.Deserialize(fs);
            }
            using (FileStream fs = new FileStream(fileRG, FileMode.OpenOrCreate))
            {
                spectreMaskRG = (int[,])formatter.Deserialize(fs);
            }
            Bitmap new_image = ImageProcessor.SegmentateImage(image, spectreMaskS, spectreMaskM, spectreMaskME, spectreMaskTE, spectreMaskRG);
            pictureBox2.Image = new_image;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string inPath = @"output\";
            string[] files = Directory.GetFiles(inPath);
            string className = textBox2.Text;
            List<double[,]> spectres = new List<double[,]>();
            foreach (string fileName in files)
            {
                if (fileName.Contains("_M_" + className))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    List<double[,]> spectres_temp = new List<double[,]>();
                    using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                    {
                        spectres_temp = (List<double[,]>)formatter.Deserialize(fs);
                        //spectres.Concat((List<double[,]>)formatter.Deserialize(fs));
                    }
                    spectres = spectres.Concat(spectres_temp).ToList();
                }
            }
            int[] count = new int[100];
            int x = Convert.ToInt32(textBox3.Text);
            int y = Convert.ToInt32(textBox4.Text);
            int j = 0;
            foreach (double[,] spectre in spectres)
            {
                if (spectre == null)
                {

                }
                else
                {
                    int value = (int)spectre[x, y];
                    count[value]++;
                    j++;
                }
            }
            Graph graph = new Graph();
            GraphPane pane = graph.zedGraphControl1.GraphPane;
            PointPairList list = new PointPairList();
            for (int i = 0; i < 100; i++)
            {
                // добавим в список точку
                list.Add(i, count[i] * 110);

            }
            LineItem myCurve = pane.AddCurve("x = " + x.ToString() + " y = " + y.ToString(), list, Color.Blue, SymbolType.None);
            pane.XAxis.MajorGrid.IsVisible = true;
            pane.XAxis.MajorGrid.DashOn = 10;
            pane.XAxis.MajorGrid.DashOff = 5;
            pane.YAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.DashOn = 10;
            pane.YAxis.MajorGrid.DashOff = 5;
            pane.YAxis.MinorGrid.IsVisible = true;
            pane.YAxis.MinorGrid.DashOn = 1;
            pane.YAxis.MinorGrid.DashOff = 2;
            pane.XAxis.MinorGrid.IsVisible = true;
            pane.XAxis.MinorGrid.DashOn = 1;
            pane.XAxis.MinorGrid.DashOff = 2;
            graph.zedGraphControl1.AxisChange();
            graph.zedGraphControl1.Invalidate();
            graph.Show();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            string inputImage = @"input\te.bmp";
            Bitmap image = new Bitmap(inputImage);
            pictureBox1.Image = image;
            List<double[,]> spectreMaskS = new List<double[,]>();
            List<double[,]> spectreMaskTe = new List<double[,]>();
            List<double[,]> spectreMaskRg = new List<double[,]>();
            List<double[,]> spectreMaskM = new List<double[,]>();
            List<double[,]> spectreMaskBg = new List<double[,]>();
            string fileTe = @"output\spectres_3_M_te_8.bin";
            string fileS = @"output\spectres_test_M_s_8.bin";
            string fileRg = @"output\spectres_3_M_rg_8.bin";
            string fileM = @"output\spectres_3_M_m_8.bin";
            string fileBg = @"output\spectres_test_M_bg_8.bin";
            BinaryFormatter formatter = new BinaryFormatter();
            
            using (FileStream fs = new FileStream(fileS, FileMode.OpenOrCreate))
            {
                spectreMaskS = (List<double[,]>)formatter.Deserialize(fs);
            }
            
            using (FileStream fs = new FileStream(fileTe, FileMode.OpenOrCreate))
            {
                spectreMaskTe = (List<double[,]>)formatter.Deserialize(fs);
            }
            spectreMaskTe.RemoveRange(1000, spectreMaskTe.Count - 1001);
            
            /*
            using (FileStream fs = new FileStream(fileRg, FileMode.OpenOrCreate))
            {
                spectreMaskRg = (List<double[,]>)formatter.Deserialize(fs);
            }*/
            
            using (FileStream fs = new FileStream(fileM, FileMode.OpenOrCreate))
            {
                spectreMaskM = (List<double[,]>)formatter.Deserialize(fs);
            }
            using (FileStream fs = new FileStream(fileBg, FileMode.OpenOrCreate))
            {
                spectreMaskBg = (List<double[,]>)formatter.Deserialize(fs);
            }
            Bitmap new_image = ImageProcessor.SegmentateImageS(image, spectreMaskS, spectreMaskM, spectreMaskRg, spectreMaskTe, spectreMaskBg);
            pictureBox2.Image = new_image;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            string inputImage1 = @"input\Compare.bmp";
            string inputMask = @"input\Compare_m.bmp";
            Bitmap image = new Bitmap(inputImage1);
            Bitmap mask = new Bitmap(inputMask);
            int p_c = 0;
            int t_c = 0;
            int m_c = 0;
            for(int x = 0; x < image.Width; x++)
            {
                for(int y = 0; y < image.Height; y++)
                {
                    if(mask.GetPixel(x, y) == Color.FromArgb(30, 30, 30))
                    {
                        image.SetPixel(x, y, Color.Red);
                    }
                }
            }
            pictureBox2.Image = image;
            image.Save(@"output\s.bmp");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            string inputImage = @"input\GA.bmp";
            string inputMask = @"input\GA_m.bmp";
            Bitmap iImage = new Bitmap(inputImage);
            Bitmap iMask = new Bitmap(inputMask);
            Color mColor = Color.FromArgb(96, 96, 64);
            bool[,] temp;
            int[,] spectreMaskM;
            string fileM = @"output\Max_Count_spectre_values_rg_Magnitude_8.bin";
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(fileM, FileMode.OpenOrCreate))
            {
                spectreMaskM = (int[,])formatter.Deserialize(fs);
            }
            double[,] sm = new double[8, 8];
            for(int x = 0; x < 8; x++)
            {
                for(int y = 0; y < 8; y++)
                {
                    sm[x, y] = spectreMaskM[x, y];
                }    
            }
            temp = ImageProcessor.GeneticAlgorithm(sm, iImage, iMask, mColor, 50, 50);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            string inputImage = @"input\3.bmp";
            Bitmap image = new Bitmap(inputImage);
            pictureBox1.Image = image;
            int[,] spectreMaskM;
            string fileM = @"output\Max_Count_spectre_values_te_Magnitude_8.bin";
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(fileM, FileMode.OpenOrCreate))
            {
                spectreMaskM = (int[,])formatter.Deserialize(fs);
            }
            Bitmap new_image = ImageProcessor.SegmentateImageM(image, spectreMaskM);
            pictureBox2.Image = new_image;
        }
        //96 79
    }
}
