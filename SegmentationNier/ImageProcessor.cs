using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
namespace SegmentationNier
{
    class ImageProcessor
    {
        private Dictionary<string, Color> masks = new Dictionary<string, Color>
        {
            ["dzn"] = Color.FromArgb(10, 10, 10),
            ["m"] = Color.FromArgb(20, 20, 20),
            ["s"] = Color.FromArgb(30, 30, 30),
            ["ns"] = Color.FromArgb(40, 40, 40),
            ["te"] = Color.FromArgb(50, 50, 50),
            ["me"] = Color.FromArgb(60, 60, 60),
            ["sc"] = Color.FromArgb(70, 70, 70),
            ["pc"] = Color.FromArgb(80, 80, 80),
            ["rg"] = Color.FromArgb(90, 90, 90),
            ["bg"] = Color.FromArgb(0, 0, 0)
        };
        private Dictionary<Color, int> masksReverse = new Dictionary<Color, int>
        {

            [Color.FromArgb(10, 10, 10)] = 0,
            [Color.FromArgb(20, 20, 20)] = 1,
            [Color.FromArgb(30, 30, 30)] = 2,
            [Color.FromArgb(40, 40, 40)] = 3,
            [Color.FromArgb(50, 50, 50)] = 4,
            [Color.FromArgb(60, 60, 60)] = 5,
            [Color.FromArgb(70, 70, 70)] = 6,
            [Color.FromArgb(80, 80, 80)] = 7,
            [Color.FromArgb(90, 90, 90)] = 8
        };
        private Dictionary<int, string> indexMasks = new Dictionary<int, string>
        {
            [0] = "dzn",
            [1] = "m",
            [2] = "s",
            [3] = "ns",
            [4] = "te",
            [5] = "me",
            [6] = "sc",
            [7] = "pc",
            [8] = "rg"
        };
        public static int counter = 0;
        public static List<double[,]> spectres = new List<double[,]>();
        /*
        private Dictionary<Color, int> masksReverse = new Dictionary<Color, string>
        {

            [Color.FromArgb(10, 10, 10)] = "dzn",
            [Color.FromArgb(20, 20, 20)] = "m",
            [Color.FromArgb(30, 30, 30)] = "s",
            [Color.FromArgb(40, 40, 40)] = "ns",
            [Color.FromArgb(50, 50, 50)] = "te",
            [Color.FromArgb(60, 60, 60)] = "me",
            [Color.FromArgb(70, 70, 70)] = "sc",
            [Color.FromArgb(80, 80, 80)] = "pc",
            [Color.FromArgb(90, 90, 90)] = "rg",
        };*/
        private Dictionary<string, List<Point>> classPoints = new Dictionary<string, List<Point>>
        {
            ["dzn"] = new List<Point>(),
            ["m"] = new List<Point>(),
            ["s"] = new List<Point>(),
            ["ns"] = new List<Point>(),
            ["te"] = new List<Point>(),
            ["me"] = new List<Point>(),
            ["sc"] = new List<Point>(),
            ["pc"] = new List<Point>(),
            ["rg"] = new List<Point>(),
        };
        private Bitmap imageSource;
        private Bitmap imageMask;
        private float accuracy;
        private static Mutex mutexFrag = new Mutex();
        public float Accuracy { get => accuracy; set => accuracy = value; }
        public Bitmap ImageSource { get => imageSource; set => imageSource = value; }
        public Bitmap ImageMask { get => imageMask; set => imageMask = value; }
        public MaskPoint[] MakePoints(Bitmap mask, int size, string className, float accuracy)
        {
            List<MaskPoint> temp = new List<MaskPoint>();
            for (int x = 0; x < mask.Width - size; x++)
            {
                for (int y = 0; y < mask.Height - size; y++)
                {
                    if (mask.GetPixel(x, y) == masks[className])
                    {
                        int countPixels = 0;
                        for (int xm = x; xm < x + size; xm++)
                        {
                            for (int ym = y; ym < y + size; ym++)
                            {
                                if (mask.GetPixel(xm, ym) == masks[className]) countPixels++;
                            }
                        }
                        if ((float)countPixels / (size * size) > accuracy)
                        {
                            MaskPoint pointTemp = new MaskPoint();
                            pointTemp.X = x;
                            pointTemp.Y = y;
                            temp.Add(pointTemp);
                        }
                    }
                }
            }
            MaskPoint[] ans = new MaskPoint[temp.Count];
            int i = 0;
            foreach (MaskPoint pointTemp in temp)
            {
                ans[i] = pointTemp;
                i++;
            }
            return ans;
        }
        public static bool[,] GeneticAlgorithm(double[,] spectre, Bitmap test, Bitmap mask, Color maskValue, int zSize, int nGeneration)
        {
            List<bool[,]> zeroGeneration = new List<bool[,]>();
            zeroGeneration = GenerateZero(zSize);
            for(int i = 0; i < nGeneration; i++)
            {
                List<bool[,]> newGeneration = new List<bool[,]>();
                for (int j = 0; j < zeroGeneration.Count; j++)
                {
                    bool[,] c;
                    bool[,] a = Sel(zeroGeneration, spectre, test, mask, maskValue);
                    bool[,] b = Sel(zeroGeneration, spectre, test, mask, maskValue);
                    while (a.Equals(b))
                    {
                        a = Sel(zeroGeneration, spectre, test, mask, maskValue);
                        b = Sel(zeroGeneration, spectre, test, mask, maskValue);
                    }
                    if (Rul(0.9))
                    {
                        c = Cross(a, b);
                    }
                    else
                    {
                        if(Rul(0.5))
                        {
                            c = a;
                        }
                        else
                        {
                            c = b;
                        }
                    }
                    if (Rul(0.1))
                    {
                        Mut(c);
                    }
                    newGeneration.Add(c);
                }
                zeroGeneration = newGeneration;
            }
            int maxI = 0;
            double maxValue = 0;
            for(int i = 0; i < zeroGeneration.Count; i++)
            {
                double temp = calculateValue(zeroGeneration[i], spectre, test, mask, maskValue);
                if(temp >= maxValue)
                {
                    maxValue = temp;
                    maxI = i;
                }
            }
            return zeroGeneration[maxI];
        }
        private static void Mut(bool[,] c)
        {
            int x = R(0, 7);
            int y = R(0, 7);
            c[x, y] = !c[x, y];
        }
        private static bool[,] Cross(bool[,] a, bool[,] b)
        {
            bool[,] c = new bool[8, 8];
            for(int x = 0; x < 8; x++)
            {
                for(int y = 0; y < 8; y++)
                {
                    if(Rul(0.5))
                    {
                        c[x, y] = (a[x, y] && b[x, y]);
                    }    
                    else
                    {
                        c[x, y] = (a[x, y] || b[x, y]);
                    }
                }
            }
            return c;
        }
        private static int R(int a, int b)
        {
            double d = 1.0 / ((double)(b - a + 1));
            Random r = new Random();
            double rnd = r.NextDouble();
            int i = 1;
            while(true)
            {
                if((i-1)*d <= rnd && rnd <= i*d)
                {
                    return (a + (i - 1));
                }
                else
                {
                    i++;
                }
            }
        }
        private static bool Rul(double p)
        {
            Random r = new Random();
            double pt = r.NextDouble();
            if(pt < p)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static bool[,] Sel(List<bool[,]> gen, double[,] spectre, Bitmap test, Bitmap mask, Color maskValue)
        {
            List<double> values = new List<double>();
            foreach(bool[,] ch in gen)
            {
                values.Add(calculateValue(ch, spectre, test, mask, maskValue));
            }
            double sum = values.Sum();
            Random random = new Random();
            double rnd = random.NextDouble();
            double g = 1.0;
            int t = gen.Count - 1;
            while(t > 0)
            {
                if((g - values[t]/sum) <= rnd)
                {
                    return gen[t];
                }
                t--;
            }
            return gen[t];
        }
        public static double calculateValue(bool[,] ch, double[,] spectre, Bitmap test, Bitmap mask, Color maskValue)
        {
            List<MaskPoint> points = new List<MaskPoint>();
            List<MaskPoint> SegmPoints = new List<MaskPoint>();
            List<MaskPoint> truePoints = new List<MaskPoint>();
            for(int x = 0; x < mask.Width; x++)
            {
                for(int y = 0; y < mask.Height; y++)
                {
                    if(mask.GetPixel(x, y) == maskValue)
                    {
                        MaskPoint tPoint = new MaskPoint();
                        tPoint.X = x;
                        tPoint.Y = y;
                        truePoints.Add(tPoint);
                    }
                }
            }
            for (int x = 0; x < 8; x++)
            {
                for(int y = 0; y < 8; y++)
                {
                    if(ch[x,y])
                    {
                        MaskPoint temp = new MaskPoint();
                        temp.X = x;
                        temp.Y = y;
                        points.Add(temp);
                    }
                }
            }
            for (int x = 0; x < test.Width - 8; x++)
            {
                for(int y = 0; y < test.Width - 8; y++)
                {
                    Complex32[,] spectreC = new Complex32[8, 8];
                    int i = 0; int j = 0;
                    for (int xs = x; xs < x + 8; xs++)
                    {
                        for (int ys = y; ys < y + 8; ys++)
                        {
                            spectreC[i, j] = test.GetPixel(xs, ys).GetBrightness();
                            j++;
                        }
                        j = 0;
                        i++;
                    }
                    Complex32[,] calculatedSpectre = FFT2d(spectreC, 0, 8, 8, 64);
                    double[,] spectreDouble = new double[8, 8];
                    for (int k = 0; k < 8; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            spectreDouble[k, l] = calculatedSpectre[k, l].Magnitude;
                        }
                    }
                    int count = 0;
                    foreach(MaskPoint p in points)
                    {
                        int k = p.X;
                        int l = p.Y;
                        double e = Math.Abs(spectreDouble[k, l] - spectre[k, l]);
                        if (e < 3)
                        {
                            count++;
                        }
                    }
                    double d = ((double)count) / points.Count;
                    if(d >= 0.6)
                    {
                        MaskPoint sPoint = new MaskPoint();
                        sPoint.X = x;
                        sPoint.Y = y;
                        SegmPoints.Add(sPoint);
                    }
                }
            }
            int dCount = 0;
            for(int i = 0; i < SegmPoints.Count; i++)
            {
                bool contains = CheckCont(SegmPoints[i], truePoints);
                if(contains)
                {
                    dCount++;
                }
                else
                {
                    dCount--;
                }
            }
            double ans = (double)dCount / (double)truePoints.Count;
            return ans;
        }
        private static bool CheckCont(MaskPoint e, List<MaskPoint> v)
        {
            foreach(MaskPoint temp in v)
            {
                if(e.X == temp.X && e.Y == temp.Y)
                {
                    return true;
                }
            }
            return false;
        }
        private static List<bool[,]> GenerateZero(int zSize)
        {
            List<bool[,]> zeroGeneration = new List<bool[,]>();
            Random random = new Random();
            for(int i = 0; i < zSize; i++)
            {
                bool[,] temp = new bool[8, 8];
                for(int x = 0; x < 8; x++)
                {
                    for(int y = 0; y < 8; y++)
                    {
                        int check = random.Next(0, 2);
                        if(check == 1)
                        {
                            temp[x, y] = true;  
                        }
                        else
                        {
                            temp[x, y] = false;
                        }
                    }
                }
                zeroGeneration.Add(temp);
            }
            return zeroGeneration;
        }
        public static void ParseSpectreFromFile(object info)
        {
            int n = ((PathSpectreInfo)info).n;
            string[] lines = new string[n];
            string Path = ((PathSpectreInfo)info).InPath;
            using (StreamReader sr = new StreamReader(Path, System.Text.Encoding.Default))
            {
                string line;
                int i = 0;
                while((line = sr.ReadLine()) != null)
                {
                    lines[i] = line;
                    i++;
                }
            }
            //Regex doubleNumber = new Regex(@"[^,](?:-)?\d*[,]\d*");
            Regex doubleNumber = new Regex(@"[^,](?:-)?\d*(?:[,]\d*)?");
            double[,] spectre = new double[n, n];
            for(int x = 0; x < n; x++)
            {
                MatchCollection matchCollection = doubleNumber.Matches(lines[x]);
                for(int y = 0; y < n; y++)
                {
                    spectre[x, y] = Convert.ToDouble(matchCollection[y].Value);
                }
            }
            counter++;
            spectres.Add(spectre);
        }

        public void FragWindows(Bitmap image, MaskPoint[] points, int size, string outPath)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Bitmap temp = new Bitmap(size, size);
                int k = 0; int l = 0;
                for (int x = points[i].X; x < points[i].X + size; x++)
                {
                    for (int y = points[i].Y; y < points[i].Y + size; y++)
                    {
                        temp.SetPixel(k, l, image.GetPixel(x, y));
                        l++;
                    }
                    l = 0;
                    k++;
                }
                temp.Save(outPath + @"\" + i.ToString() + ".bmp");
                temp.Dispose();
            }
        }
        public void makePoints(object maskInfo)
        {
            MaskInfo mask = (MaskInfo)maskInfo;
            Color maskColor = masks[mask.maskName];
            int winSize = mask.windowSize;
            mutexFrag.WaitOne();
            int xMax = ImageMask.Width - winSize;
            mutexFrag.ReleaseMutex();
            for (int x = 0; x < xMax - winSize; x++)
            {
                Column col = new Column();
                col.colNum = x;
                col.maskName = mask.maskName;
                col.winSize = winSize;
                Thread colThread = new Thread(new ParameterizedThreadStart(columnAnalysis));
                colThread.Start(col);
                //colThread.Join();
            }
            Console.WriteLine("Finish");
            int i = 0;
        }
        public static Complex32[] FFT1d(Complex32[] a, float xl, float xr, int N, int M)
        {
            float hx = (xr - xl) / (N - 1);
            Complex32[] nulls = new Complex32[(M - N) / 2];
            for (int j = 0; j < (M - N) / 2; j++)
            {
                nulls[j] = new Complex32(0, 0);
            }
            Complex32[] NM = nulls.Concat(a).ToArray().Concat(nulls).ToArray();
            a = NM;
            int jm = a.Length / 2;
            Complex32[] al = new Complex32[jm];
            Complex32[] ar = new Complex32[jm];
            for (int j = 0; j < jm; j++)
            {
                al[j] = a[j];
            }
            int k = 0;
            for (int j = jm; j < a.Length; j++)
            {
                ar[k] = a[j];
                k++;
            }
            a = ar.Concat(al).ToArray();

            Fourier.Forward(a, FourierOptions.Matlab);

            for (int j = 0; j < a.Length; j++)
            {
                a[j] = a[j] * hx;
            }

            for (int j = 0; j < jm; j++)
            {
                al[j] = a[j];
            }
            k = 0;
            for (int j = jm; j < a.Length; j++)
            {
                ar[k] = a[j];
                k++;
            }

            a = ar.Concat(al).ToArray();

            Complex32[] ans = new Complex32[N];
            k = 0;
            for (int j = (M - N) / 2; j < (M - N) / 2 + N; j++)
            {
                ans[k] = a[j];
                k++;
            }
            a = ans;
            return a;
        }
        public static Complex32[,] FFT2d(Complex32[,] a2, float xl, float xr, int N, int M)
        {
            float bord = xl;
            for (int j = 0; j < N; j++)
            {
                Complex32[] tempRow = new Complex32[N];
                for (int k = 0; k < N; k++)
                {
                    tempRow[k] = a2[j, k];
                }
                tempRow = FFT1d(tempRow, xl, xr, N, M);
                for (int k = 0; k < N; k++)
                {
                    a2[j, k] = tempRow[k];
                }
            }
            for (int j = 0; j < N; j++)
            {
                Complex32[] tempColumn = new Complex32[N];
                for (int k = 0; k < N; k++)
                {
                    tempColumn[k] = a2[k, j];
                }
                tempColumn = FFT1d(tempColumn, xl, xr, N, M);
                for (int k = 0; k < N; k++)
                {
                    a2[k, j] = tempColumn[k];
                }
            }
            Complex32[,] copy = new Complex32[N, N];
            for (int j = 0; j < N; j++)
            {
                for (int k = 0; k < N; k++)
                {
                    copy[j, k] = a2[j, k];
                }
            }
            a2 = copy;
            //borders = bord;
            return a2;
        }
        public static int kNear(double[,] sp, List<double[,]> s, List<double[,]> m, List<double[,]> rg, List<double[,]> te, List<double[,]> bg, int k)
        {
            Comparison<double[,]> compSpectres = (double[,] one, double[,] two) =>
            {
                double oned = pEuclid(sp, one);
                double twod = pEuclid(sp, two);
                if (oned < twod) return -1;
                else
                {
                    if (oned > twod) return 1;
                    else return 0;
                }
            };
            List<double[,]> spectres = new List<double[,]>();
            Dictionary<double[,], int> s_c = new Dictionary<double[,], int>();
            foreach(double[,] temp in s)
            {
                spectres.Add(temp);
                s_c.Add(temp, 1);
            }
            foreach (double[,] temp in m)
            {
                spectres.Add(temp);
                s_c.Add(temp, 2);
            }
            foreach (double[,] temp in rg)
            {
                spectres.Add(temp);
                s_c.Add(temp, 3);
            }
            foreach (double[,] temp in te)
            {
                spectres.Add(temp);
                s_c.Add(temp, 4);
            }
            foreach (double[,] temp in bg)
            {
                spectres.Add(temp);
                s_c.Add(temp, 5);
            }
            spectres.Sort(compSpectres);
            int[] counter = new int[5];
            for (int i = 0; i < k; i++)
            {
                int type = s_c[spectres[i]];
                switch (type)
                {
                    case 1:
                        counter[0]++;
                        break;
                    case 2:
                        counter[1]++;
                        break;
                    case 3:
                        counter[2]++;
                        break;
                    case 4:
                        counter[3]++;
                        break;
                    case 5:
                        counter[4]++;
                        break;
                }
            }
            bool check = false;
            int j = 0;
            while (!check)
            {
                if (counter[j] == counter.Max())
                {
                    check = true;
                }
                else
                {
                    j++;
                }
            }
            return j;
        }
        private static double pEuclid(double[,] sp, double[,] m)
        {
            double sum = 0;
            for(int x = 0; x < 3; x++)
            {
                for(int y = 0; y < 3; y++)
                {
                    sum += (sp[x, y] - m[x, y]) * (sp[x, y] - m[x, y]);
                }
            }
           
            sum = Math.Sqrt(sum);
            return sum;
        }
        public static Bitmap SegmentateImage(Bitmap image, int[,] spectreMaskS, int[,] spectreMaskM, int[,] spectreMaskMe, int[,] spectreMaskTE, int[,] spectreMaskRG)
        {
            Bitmap outputImage = (Bitmap)image.Clone();
            for(int x = 0; x < image.Width - 10; x++)
            {
                for(int y = 0; y < image.Height - 10; y++)
                {
                    Complex32[,] spectre = new Complex32[10, 10];
                    int i = 0; int j = 0;
                    for (int xs = x; xs < x + 10; xs++)
                    {
                        for (int ys = y; ys < y + 10; ys++)
                        {
                            spectre[i, j] = image.GetPixel(xs, ys).GetBrightness();
                            j++;
                        }
                        j = 0;
                        i++;
                    }
                    Complex32[,] calculatedSpectre = FFT2d(spectre, 0, 10, 10, 128);
                    double[,] spectreDouble = new double[10, 10];
                    for(int k = 0; k < 10; k++)
                    {
                        for(int l = 0; l < 10; l++)
                        {
                            spectreDouble[k, l] = calculatedSpectre[k, l].Magnitude;
                        }
                    }
                    int[,] checkMatrix = new int[10, 10];
                    int s_count = 0; int m_count = 0; int me_count = 0; int te_count = 0; int rg_count = 0;
                    for (int k = 0; k < 10; k++)
                    {
                        for (int l = 0; l < 10; l++)
                        {
                            double[] r = new double[5];
                            r[0] = Math.Abs(spectreDouble[k, l] - spectreMaskS[k, l]);
                            r[1] = Math.Abs(spectreDouble[k, l] - spectreMaskM[k, l]);
                            r[2] = Math.Abs(spectreDouble[k, l] - spectreMaskMe[k, l]);
                            r[3] = Math.Abs(spectreDouble[k, l] - spectreMaskTE[k, l]);
                            r[4] = Math.Abs(spectreDouble[k, l] - spectreMaskRG[k, l]);
                            int v_index = 0;
                            bool check = false;
                            while (!check)
                            {
                                if (r[v_index] == r.Min())
                                {
                                    check = true;
                                }
                                else
                                {
                                    v_index++;
                                }
                            }
                            checkMatrix[k, l] = v_index;
                            switch (v_index)
                            {
                                case 0:
                                    if (r[0] < 2)
                                    {
                                        s_count++;
                                    }
                                    break;
                                case 1:
                                    if (r[1] < 3)
                                    {
                                        m_count++;
                                    }
                                    break;
                                case 2:
                                    if (r[2] < 2)
                                    {
                                        me_count++;
                                    }
                                    break;
                                case 3:
                                    if (r[3] < 3)
                                    {
                                        te_count++;
                                    }
                                    break;
                                case 4:
                                    if (r[4] < 3)
                                    {
                                        rg_count++;
                                    }
                                    break;
                            }
                        }
                    }
                    if (s_count >= 80 || m_count >= 40 || me_count >= 80 || te_count >= 60 || rg_count >= 60 )
                    {
                        if(s_count >= 80)
                        {
                            for (int xs = x; xs < x + 10; xs++)
                            {
                                for (int ys = y; ys < y + 10; ys++)
                                {
                                    outputImage.SetPixel(xs,ys, Color.Red);
                                    j++;
                                }
                                j = 0;
                                i++;
                            }
                        }
                        if (m_count >= 40)
                        {
                            for (int xs = x; xs < x + 10; xs++)
                            {
                                for (int ys = y; ys < y + 10; ys++)
                                {
                                    outputImage.SetPixel(xs, ys, Color.Blue);
                                    j++;
                                }
                                j = 0;
                                i++;
                            }
                        }
                        if (me_count >= 60)
                        {
                            for (int xs = x; xs < x + 10; xs++)
                            {
                                for (int ys = y; ys < y + 10; ys++)
                                {
                                    image.SetPixel(xs, ys, Color.Purple);
                                    j++;
                                }
                                j = 0;
                                i++;
                            }
                        }
                        if (te_count >= 60)
                        {
                            for (int xs = x; xs < x + 10; xs++)
                            {
                                for (int ys = y; ys < y + 10; ys++)
                                {
                                    outputImage.SetPixel(xs, ys, Color.Yellow);
                                    j++;
                                }
                                j = 0;
                                i++;
                            }
                        }
                        if (rg_count >= 60)
                        {
                            for (int xs = x; xs < x + 10; xs++)
                            {
                                for (int ys = y; ys < y + 10; ys++)
                                {
                                    outputImage.SetPixel(xs, ys, Color.Orange);
                                    j++;
                                }
                                j = 0;
                                i++;
                            }
                        }
                    }
                }
            }
            return outputImage;
        }
        public static Bitmap SegmentateImageS(Bitmap image, List<double[,]> s, List<double[,]> m, List<double[,]> rg, List<double[,]> te, List<double[,]> bg)
        {
            Bitmap outputImage = (Bitmap)image.Clone();

            for (int x = 0; x < image.Width - 8; x++)
            {
                for (int y = 0; y < image.Height - 8; y++)
                {
                    Complex32[,] spectre = new Complex32[8, 8];
                    int i = 0; int j = 0;
                    for (int xs = x; xs < x + 8; xs++)
                    {
                        for (int ys = y; ys < y + 8; ys++)
                        {
                            spectre[i, j] = image.GetPixel(xs, ys).GetBrightness();
                            j++;
                        }
                        j = 0;
                        i++;
                    }
                    Complex32[,] calculatedSpectre = FFT2d(spectre, 0, 8, 8, 64);
                    double[,] spectreDouble = new double[8, 8];
                    for (int k = 0; k < 8; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            spectreDouble[k, l] = calculatedSpectre[k, l].Magnitude;
                        }
                    }
                    int type = kNear(spectreDouble, s, m, rg, te, bg, 51);
                    switch (type)
                    {
                        case 0:
                            /*
                            for (int xs = x; xs < x + 8; xs++)
                            {
                                for (int ys = y; ys < y + 8; ys++)
                                {
                                    outputImage.SetPixel(xs, ys, Color.Red);
                                    j++;
                                }
                                j = 0;
                                i++;
                            }
                            break;
                            */
                            outputImage.SetPixel(x, y, Color.Red);
                            break;
                        case 1:
                            outputImage.SetPixel(x, y, Color.Yellow);
                            break;
                        case 2:
                            outputImage.SetPixel(x, y, Color.Purple);
                            break;
                        case 3:
                            outputImage.SetPixel(x, y, Color.Green);
                            break;
                        case 4:
                            outputImage.SetPixel(x, y, Color.Green);
                            /*for (int xs = x; xs < x + 8; xs++)
                            {
                                for (int ys = y; ys < y + 8; ys++)
                                {
                                    outputImage.SetPixel(xs, ys, Color.Green);
                                    j++;
                                }
                                j = 0;
                                i++;
                            }*/
                            break;
                    }
                }
            }
            Random r = new Random();
            outputImage.Save(@"output\test_" + r.ToString() + ".bmp");
            return outputImage;
        }
        public static Bitmap SegmentateImageM(Bitmap image, int[,] spectreMaskM)
        {
            Bitmap outputImage = (Bitmap)image.Clone();
            for (int x = 0; x < image.Width - 8; x++)
            {
                for (int y = 0; y < image.Height - 8; y++)
                {
                    Complex32[,] spectre = new Complex32[8, 8];
                    int i = 0; int j = 0;
                    for (int xs = x; xs < x + 8; xs++)
                    {
                        for (int ys = y; ys < y + 8; ys++)
                        {
                            spectre[i, j] = image.GetPixel(xs, ys).GetBrightness();
                            j++;
                        }
                        j = 0;
                        i++;
                    }
                    Complex32[,] calculatedSpectre = FFT2d(spectre, 0, 8, 8, 64);
                    double[,] spectreDouble = new double[8, 8];
                    for (int k = 0; k < 8; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            spectreDouble[k, l] = calculatedSpectre[k, l].Magnitude;
                        }
                    }
                    int m_count = 0;
                    for (int k = 0; k < 8; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            double e = Math.Abs(spectreDouble[k, l] - spectreMaskM[k, l]);

                            if (e < 2)
                            {
                                m_count++;
                            }
                        }
                    }
                    if (m_count >= 30)
                    {
                        outputImage.SetPixel(x, y, Color.Yellow);
                        /*for (int xs = x; xs < x + 8; xs++)
                            {
                                for (int ys = y; ys < y + 8; ys++)
                                {
                                    outputImage.SetPixel(xs, ys, Color.Red);
                                    j++;
                                }
                                j = 0;
                                i++;
                            }*/
                    }
                }
            }
            return outputImage;
        }
        public void calculateSpectre(object Path)
        {
            string inPath = ((PathSpectreInfo)Path).InPath;
            string outPath = ((PathSpectreInfo)Path).OutPath;
            string outPathM = outPath + "_M.txt";
            string outPathP = outPath + "_P.txt";
            Bitmap image = new Bitmap(inPath);
            int N = image.Width;
            Complex32[,] spectre = new Complex32[N, N];
            for(int x = 0; x < N; x++)
            {
                for(int y = 0; y < N; y++)
                {
                    spectre[x, y] = image.GetPixel(x, y).GetBrightness();
                }
            }
            Complex32[,] calculatedSpectre = FFT2d(spectre, 0, N, N, 64);
            string outStringMagnitude = "";
            string outStringPhase = "";
            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {

                    outStringMagnitude += calculatedSpectre[x, y].Magnitude.ToString() + " ";
                    outStringPhase += calculatedSpectre[x, y].Phase.ToString() + " ";
                }
                outStringMagnitude += "\n";
                outStringPhase += "\n";
            }
            using (StreamWriter sw = new StreamWriter(outPathM, false, System.Text.Encoding.Default))
            {
                sw.Write(outStringMagnitude); 
            }
            using (StreamWriter sw = new StreamWriter(outPathP, false, System.Text.Encoding.Default))
            {
                sw.Write(outStringPhase);
            }
        }
        private void columnAnalysis(object colInfo)
        {
            Column col = (Column)colInfo;
            int x = col.colNum;
            //Console.WriteLine(x);
            int winSize = col.winSize;
            string maskName = col.maskName;
            int[] count = new int[9];
            for (int i = 0; i < 9; i++)
            {
                count[i] = 0;
            }
            mutexFrag.WaitOne();
            int yMax = ImageMask.Height - winSize;
            mutexFrag.ReleaseMutex();
            for (int y = 0; y < yMax - winSize; y++)
            {
                for(int xm = x; xm < x + winSize; xm++)
                {
                    for(int ym = y; ym < y + winSize; ym++)
                    {
                        mutexFrag.WaitOne();
                        Color maskPixel = ImageMask.GetPixel(xm, ym);
                        mutexFrag.ReleaseMutex();
                        if (maskPixel != Color.FromArgb(0, 0, 0))
                        {
                            int index = masksReverse[maskPixel];
                            count[index]++;
                        }
                    }
                }
                int maxValue = count.Max();
                if ((float)maxValue / (winSize * winSize) > Accuracy)
                {
                    int maxIndex = 0;
                    for (int i = 0; i < 9; i++)
                    {
                        if (count[i] == maxValue)
                        {
                            maxIndex = i;
                            break;
                        }
                    }
                    //Console.WriteLine(maxValue);
                    classPoints[indexMasks[maxIndex]].Add(new Point(x, y));
                }
                for (int i = 0; i < 9; i++)
                {
                    count[i] = 0;
                }
            }
        }
    }
}
