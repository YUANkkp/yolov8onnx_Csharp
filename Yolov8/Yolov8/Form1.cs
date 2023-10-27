using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using ResultSharp;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp.Dnn;

namespace Yolov8
{

    public partial class Form1 : Form
    {
        private string _modelPath;
        private InferenceSession _session;
        private string _inputName;
        //����ģ������
        public float[] RunInference(float[] inputData, int inputSize)
        {
            // ���������ݵ���Ϊ (1, 28, 28) ��״������  
            var reshapedInputData = new DenseTensor<float>(new[] { 1, 28, 28 });
            for (int i = 0; i < 28; i++)
            {
                for (int j = 0; j < 28; j++)
                {
                    reshapedInputData[0, i, j] = inputData[i * 28 + j];
                }
            }

            // �������� NamedOnnxValue  
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, reshapedInputData) };

            // ����ģ������  
            using var results = _session.Run(inputs);

            // ��ȡ�������  
            float[] outputData = results.ToArray()[0].AsEnumerable<float>().ToArray();

            return outputData;
        }

        public Form1()
        {
            InitializeComponent();
        }
        // ׼��ͼ��������Ϊģ�͵�����
        private static Tensor<float> PrepareInputImage(string imagePath)
        {
            // ��ȡͼ��
            Bitmap bitmap = new Bitmap(imagePath);

            // ����ͼ���С��ͨ��˳�򣬽���ת��Ϊģ�������ĸ�ʽ
            // �������ģ�������������ʽΪHWC���߶ȡ���ȡ�ͨ����
            int height = bitmap.Height;
            int width = bitmap.Width;
            int channels = 3; // ͨ����������ΪRGBͼ��

            float[] inputData = new float[height * width * channels];

            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    System.Drawing.Color pixel = bitmap.GetPixel(w, h);
                    inputData[h * width * channels + w * channels + 0] = pixel.R / 255.0f;
                    inputData[h * width * channels + w * channels + 1] = pixel.G / 255.0f;
                    inputData[h * width * channels + w * channels + 2] = pixel.B / 255.0f;
                }
            }

            // ����Tensor
            var inputTensor = new DenseTensor<float>(inputData, new[] { 1, height, width, channels });

            return inputTensor;
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            string model_path = tb_model_path.Text;
            //model_path = @"E:\Git_space\����Csharp����Yolov8\model\yolov8s.engine";
            //model_path = @"E:\Git_space\Csharp_deploy_Yolov8\model\yolov8s.onnx";
            //model_path = @"E:\Git_space\Csharp_deploy_Yolov8\model\yolov8s-seg.onnx";
            //model_path = @"E:\Git_space\Csharp_deploy_Yolov8\model\yolov8s-pose.onnx";

            string classer_path = tb_clas_path.Text;
            //classer_path = @"E:\Git_space\Csharp_deploy_Yolov8\demo\det_lable.txt";
            //classer_path = @"E:\Git_space\Csharp_deploy_Yolov8\demo\cls_lable.txt";
            string image_path = tb_test_image.Text;
            //image_path = @"E:\Git_space\Csharp_deploy_Yolov8\demo\demo_9.jpg";


            DateTime begin = DateTime.Now;
            DateTime end = DateTime.Now;
            TimeSpan model_load = new TimeSpan(0, 0, 0);
            TimeSpan data_load = new TimeSpan(0, 0, 0);
            TimeSpan model_infer = new TimeSpan(0, 0, 0);
            TimeSpan result_process = new TimeSpan(0, 0, 0);

            begin = DateTime.Now;
            // ����ͼƬ����
            Mat image = new Mat(image_path);
            int max_image_length = image.Cols > image.Rows ? image.Cols : image.Rows;
            Mat max_image = Mat.Zeros(new OpenCvSharp.Size(max_image_length, max_image_length), MatType.CV_8UC3);
            Rect roi = new Rect(0, 0, image.Cols, image.Rows);
            image.CopyTo(new Mat(max_image, roi));
            end = DateTime.Now;
            data_load = end.Subtract(begin);

            float[] result_array = new float[8400 * 5];
            float[] factors = new float[2];
            factors = new float[2];
            factors[0] = factors[1] = (float)(max_image_length / 640.0);


            Console.WriteLine("------Yolov8 detection model deploy ONNX runtime-------");
            begin = DateTime.Now;
            // ��������Ự���������ģ�Ͷ�ȡ��Ϣ
            SessionOptions options = new SessionOptions();
            options.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_INFO;
            // ����ΪCPU������
            options.AppendExecutionProvider_CPU(0);

            // ��������ģ���࣬��ȡ����ģ���ļ�
            InferenceSession onnx_session = new InferenceSession(model_path, options);//model_path Ϊonnxģ���ļ���·��
            end = DateTime.Now;
            model_load = end.Subtract(begin);
            begin = DateTime.Now;
            // ��ͼƬתΪRGBͨ��
            Mat image_rgb = new Mat();
            Cv2.CvtColor(max_image, image_rgb, ColorConversionCodes.BGR2RGB);
            Mat resize_image = new Mat();
            Cv2.Resize(image_rgb, resize_image, new OpenCvSharp.Size(640, 640));

            // ��������Tensor
            Tensor<float> input_tensor = new DenseTensor<float>(new[] { 1, 3, 640, 640 });
            for (int y = 0; y < resize_image.Height; y++)
            {
                for (int x = 0; x < resize_image.Width; x++)
                {
                    input_tensor[0, 0, y, x] = resize_image.At<Vec3b>(y, x)[0] / 255f;
                    input_tensor[0, 1, y, x] = resize_image.At<Vec3b>(y, x)[1] / 255f;
                    input_tensor[0, 2, y, x] = resize_image.At<Vec3b>(y, x)[2] / 255f;
                }
            }

            // ������������
            List<NamedOnnxValue> input_ontainer = new List<NamedOnnxValue>();
            //�� input_tensor ����һ�������������������ָ������
            input_ontainer.Add(NamedOnnxValue.CreateFromTensor("images", input_tensor));
            end = DateTime.Now;
            data_load += end.Subtract(begin);
            begin = DateTime.Now;
            //���� Inference ����ȡ���
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> result_infer = onnx_session.Run(input_ontainer);
            end = DateTime.Now;
            model_infer += end.Subtract(begin);
            begin = DateTime.Now;
            // ��������תΪDisposableNamedOnnxValue����
            DisposableNamedOnnxValue[] results_onnxvalue = result_infer.ToArray();

            // ��ȡ��һ���ڵ������תΪTensor����
            Tensor<float> result_tensors = results_onnxvalue[0].AsTensor<float>();

            result_array = result_tensors.ToArray();
            onnx_session.Dispose();
            resize_image.Dispose();
            image_rgb.Dispose();


            Console.WriteLine("ģ�ͼ���ʱ�䣺{0}\n", model_load.TotalMilliseconds);
            Console.WriteLine("���ݼ���ʱ�䣺{0}\n", data_load.TotalMilliseconds);
            Console.WriteLine("ģ������ʱ�䣺{0}\n", model_infer.TotalMilliseconds);
            Console.WriteLine("�������ʱ�䣺{0}\n", result_process.TotalMilliseconds);

            MessageBox.Show("ģ������ʱ�䣺{0}\n", (model_infer.TotalMilliseconds+ result_process.TotalMilliseconds).ToString());

            DetectionResult result_pro = new DetectionResult(classer_path, factors);
            Mat result_image = result_pro.draw_result(result_pro.process_result(result_array), image.Clone());
            end = DateTime.Now;
            result_process += end.Subtract(begin);


            Cv2.Resize(image, image, new OpenCvSharp.Size(512, 612));
            Cv2.Resize(result_image, result_image, new OpenCvSharp.Size(512, 612));
            pictureBox1.Image = new Bitmap(image.ToMemoryStream()) as Image;
            pictureBox2.Image = new Bitmap(result_image.ToMemoryStream()) as Image;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            //��Ҫ�ı�Ի������
            dlg.Title = "ѡ������ģ���ļ�";
            //ָ����ǰĿ¼
            //dlg.InitialDirectory = System.Environment.CurrentDirectory;
            //dlg.InitialDirectory = System.IO.Path.GetFullPath(@"..//..//..//..");
            //�����ļ�����Ч��
            dlg.Filter = "ģ���ļ�(*.pt,*.onnx,*.engine)|*.pt;*.onnx;*.engine";
            dlg.InitialDirectory = @"E:\Git_space\Csharp_deploy_Yolov8\model";
            //�ж��ļ��Ի����Ƿ��
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tb_model_path.Text = dlg.FileName;
            }
        }

        private void btn_choose_claspath_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            //��Ҫ�ı�Ի������
            dlg.Title = "ѡ������ļ�";
            //ָ����ǰĿ¼
            //dlg.InitialDirectory = System.Environment.CurrentDirectory;
            //dlg.InitialDirectory = System.IO.Path.GetFullPath(@"..//..//..//..");
            //�����ļ�����Ч��
            dlg.Filter = "�����ļ�(*.txt)|*.txt";
            dlg.InitialDirectory = @"E:\Git_space\Csharp_deploy_Yolov8\demo";
            //�ж��ļ��Ի����Ƿ��
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tb_clas_path.Text = dlg.FileName;
            }
        }

        private void btn_choose_testimage_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            //��Ҫ�ı�Ի������
            dlg.Title = "ѡ�����ͼƬ�ļ�";
            //ָ����ǰĿ¼
            //dlg.InitialDirectory = System.Environment.CurrentDirectory;
            //dlg.InitialDirectory = System.IO.Path.GetFullPath(@"..//..//..//..");
            //�����ļ�����Ч��
            dlg.Filter = "ͼƬ�ļ�(*.png,*.jpg,*.jepg)|*.png;*.jpg;*.jepg";
            dlg.InitialDirectory = @"E:\Git_space\Csharp_deploy_Yolov8\demo";
            //�ж��ļ��Ի����Ƿ��
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tb_test_image.Text = dlg.FileName;
            }
        }
    }
}