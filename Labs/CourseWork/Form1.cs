using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Npgsql;
using NeuralNetwork;
using System.IO;


namespace CourseWork
{
    public partial class Form1 : Form
    {
        string[] ParametersNames; // назви параметрів керогену
        Stopwatch sWatch = new Stopwatch(); // клас для вимірювання часу навчання мережі Delta-Bar-Delta
        Stopwatch sWatch2 = new Stopwatch(); // клас для вимірювання часу навчання мережі LVQ
        TimeSpan tSpan; // клас для перетворення даних про час навчання

        // змінні для створення таблиць із результатами роботи мережі (DeltaBarDelta)
        DataTable ResultTrainDeltaBarDelta; // результати роботи DeltaBarDelta для навчальної вибірки
        DataTable ResultTestDeltaBarDelta; //  результати роботи DeltaBarDelta для тестової вибірки

        // змінні для створення таблиць із результатами роботи мережі (LVQ)
        DataTable ResultTrainLVQ; // результати роботи LVQ для навчальної вибірки
        DataTable ResultTestLVQ; //  результати роботи LVQ для тестової вибірки

        // навчальна вибірка
        int TRAINING_PATTERNS; // кількість паттернів у навчальній вибірці
        int PARAMETERS; // кількість параметрів пласта 
        const int NUM_OF_CLUSTERS = 2; // кількість типів пласта
        int TestAmount; // кількість випадкової вибірки для тестування мереж

        // Параметри DeltaBarDelta
        int[] layerSizes; // кількість шарів та нейронів у шарах
        // активаційні функції для кожного шару
        TransferFunction[] TFuncs = new TransferFunction[3] {TransferFunction.None,
                                                               TransferFunction.Sigmoid,
                                                               TransferFunction.Sigmoid};
        double LEARNING_RATE1; // швидкість навчання 
        double MOMENTUM; // коефіцієнт для навчання
        double MIN_ERROR; // мінімальна похибка для навчання
        double kCoef; // коефіцієнт k для зміни ваги
        double betaCoef; // коефіцієнт b для зміни ваги
        double eCoef; // коефіцієнт e для зміни ваги
        double[] output = new double[1];

        // Параметри LVQ
        double MIN_ERROR2; // мінімальна похибка для навчання (LVQ)
        double LEARNING_RATE2; // швидкість навчання
        double DECAY_RATE; // швидкість зміни швидкості нвчання

        // масиви параметрів та відповідей
        double[][] inputs;
        double[][] answers;

        // Випадкова тестова вибірка
        double[][] testArray;

        // мережа DeltaBarDelta
        DeltaBarDelta dbd = null;

        // мережа LVQ
        LVQ lvq = null;

        public Form1()
        {
            InitializeComponent();

            InitializeInputPatterns();

            this.RandomResults.DataSource = ResultTestDeltaBarDelta;
            saveFileDialog.Filter = "XML - документи |*.xml";
            saveFileDialog2.Filter = "XML - документи |*.xml";
            saveResultsFileDialog.Filter = "Текстові документи|*.txt";
            saveFileDialog1.Filter = "Текстові документи|*.txt";
            saveFileDialog3.Filter = "Текстові документи|*.txt";
            saveFileDialog4.Filter = "Текстові документи|*.txt";

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        // ініціалізація даних для навчання нейромереж
        private void InitializeInputPatterns()
        {
            // ініціалізація вхідних параметрів (пористість, глинистість, карбонатність, амплітуда)
            double[][] trainPatterns = new double[][] {
                    new double[] { 0.189,  0.1,    0.86,   0.22},
                    new double[] { 0.141,  0.078,  0.123,  0.12,},
                    new double[] { 0.15,   0.095,  0.128,  0.08},
                    new double[] { 0.126,  0.401,  0.085,  0.04},
                    new double[] { 0.109,  0.156,  0.179,  0.08},
                    new double[] { 0.095,  0.278,  0.124,  0.05},
                    new double[] { 0.156,  0.124,  0.09,   0.17},
                    new double[] { 0.178,  0.167,  0.075,  0.05},
                    new double[] { 0.107,  0.222,  0.119,  0.14},
                    new double[] { 0.115,  0.174,  0.182,  0.07},
                    new double[] { 0.126,  0.151,  0.144,  0.1},
                    new double[] { 0.088,  0.189,  0.25,   0.03},
                    new double[] { 0.12,   0.335,  0.086,  0.03},
                    new double[] { 0.09,   0.147,  0.197,  0.07},
                    new double[] { 0.085,  0.15,   0.224,  0.04}

            };

            // ініціалізація відповідей (тип пласта: 1 - колектор, 2 - покришка)
            double[][] answers_ = new double[][]
            {
                new double[] {1},
                new double[] {1},
                new double[] {1},
                new double[] {2},
                new double[] {2},
                new double[] {2},
                new double[] {1},
                new double[] {1},
                new double[] {1},
                new double[] {1},
                new double[] {1},
                new double[] {2},
                new double[] {2},
                new double[] {2},
                new double[] {2}
            };

            TRAINING_PATTERNS = trainPatterns.GetUpperBound(0)+1;
            PARAMETERS = trainPatterns[0].Length; 

            inputs = new double[TRAINING_PATTERNS][];
            answers = new double[TRAINING_PATTERNS][];

            for (int i = 0; i < TRAINING_PATTERNS; i++)
            {
                inputs[i] = new double[PARAMETERS];
                answers[i] = new double[1];
            }

            for (int i = 0; i < TRAINING_PATTERNS; i++)
            {
                for (int k = 0; k < trainPatterns[0].Length; k++)
                    inputs[i][k] = trainPatterns[i][k];

                answers[i][0] = answers_[i][0];
            }

            ParametersNames = new string[] {"Porosity", "Clayness", "Carbonate", "Amplitude" };

        }

        // збереження мережі DeltaBarDelta у файл
        private void SaveNetwork_Click(object sender, EventArgs e)
        {
            
            saveFileDialog.ShowDialog();
        }

      

        // виклик діалогу для збереження мережі DeltaBarDelta у файл
        private void saveFileBackProp_FileOk(object sender, CancelEventArgs e)
        {
            string filepath = saveFileDialog.FileName;
            if (filepath == null || dbd == null)
                return;
            dbd.Save(filepath);
            MessageBox.Show("DeltaBarDelta has been saved");
        }

        // збереження мережі LVQ у файл
        private void SaveLVQ_Click(object sender, EventArgs e)
        {
            saveFileDialog2.ShowDialog();
        }

        // виклик діалогу для збереження мережі LVQ у файл
        private void saveFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            string filepath = saveFileDialog2.FileName;
            if (filepath == null || lvq == null)
                return;
            lvq.Save(filepath);
            MessageBox.Show("Мережа LVQ збережена");       
        }

        // генерування тестових даних
        private void GenerateRandom_Click(object sender, EventArgs e)
        {
            if(PARAMETERS == 0)
            {
                MessageBox.Show("Initial error occured");
                return;
            }
            //TestAmount = Convert.ToInt32(this.testAmountText.Text); 
            bool result = Int32.TryParse(this.testAmountText.Text, out TestAmount);
            if (result == false)
            {
                MessageBox.Show("Enter the amount of vectors with random values");
                return;
            }
            
            if (TestAmount <= 0)
            {
                MessageBox.Show("Forbidden value");
                return;
            }   
            testArray = GenerateTest.GenerateOutputICG(PARAMETERS, TestAmount); // створення тестової вибірки
           
            MessageBox.Show("Test random set has been generated");
        }

        // вихід із програми
        private void Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // створення мережі DeltaBarDelta
        private void CreateDeltaBarDelta_Click(object sender, EventArgs e)
        {
            if(PARAMETERS == 0)
            {
                MessageBox.Show("Initial error occured");
                return;
            }
            int hidden;
            bool resultHidden = Int32.TryParse(this.Hidden_Neurons.Text, out hidden);
            bool resultLearning_Rate1 = Double.TryParse(this.Learning_rate1.Text, out LEARNING_RATE1);
            bool resultMomentum = Double.TryParse(this.Momentum.Text, out MOMENTUM);
            bool resultMin_error = Double.TryParse(this.MinError1.Text, out MIN_ERROR);
            bool resultKCoef = Double.TryParse(this.kCoefficient.Text, out kCoef);
            bool resultBetaCoef = Double.TryParse(this.bCoefficient.Text, out betaCoef);
            bool resultECoef = Double.TryParse(this.eCoefficient.Text, out eCoef);

            if (resultHidden == false || resultLearning_Rate1 == false || resultMomentum == false || resultMin_error == false
                || resultKCoef == false || resultBetaCoef == false || resultECoef == false)
            {
                MessageBox.Show("Not all fields are entered for creation of Delta-Bar-Delta");
                return;
            }
         
            layerSizes = new int[3]{ PARAMETERS, hidden, 1 }; // кількість шарів та нейронів у шарах

            dbd = new DeltaBarDelta(layerSizes, TFuncs, LEARNING_RATE1);
            MessageBox.Show("Delta-Bar-Delta has been created");
        }

        // Навчання мережі DeltaBarDelta
        private void Train_Click(object sender, EventArgs e)
        {
            if (dbd == null)
            {
                MessageBox.Show("First create the network");
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            sWatch.Reset();
            sWatch.Start();
            
            dbd.TrainNetwork(inputs, Normalize.FormAnswersDeltaBarDelta(answers), MIN_ERROR, kCoef, betaCoef, eCoef, MOMENTUM);
            sWatch.Stop();
            Cursor.Current = Cursors.Arrow;

            tSpan = sWatch.Elapsed;
            this.Time1.Text = Convert.ToString(tSpan);
            MessageBox.Show("Delta-Bar-Delta has been trained");
        }

        // Створення мережі LVQ
        private void TrainLVQ_Click(object sender, EventArgs e)
        {
            if (PARAMETERS == 0)
            {
                MessageBox.Show("Спочатку завантажте дані");
                return;
            }

            bool resultLearning_rate2 = Double.TryParse(LearningRateLVQ.Text, out LEARNING_RATE2);
            bool resultMin_error2 = Double.TryParse(MinErrorLVQ.Text, out MIN_ERROR2);
            bool resultDecay_rate = Double.TryParse(DecayRateLVQ.Text, out DECAY_RATE);

            if (resultLearning_rate2 == false || resultMin_error2 == false || resultDecay_rate == false)
            {
                MessageBox.Show("Не всі обов'язкові поля заповнені для створення мережі LVQ");
                return;
            }
        
            lvq = new LVQ(inputs, Normalize.FormAnswersLVQ(answers), MIN_ERROR2, LEARNING_RATE2, DECAY_RATE, NUM_OF_CLUSTERS);
            MessageBox.Show("Мережу LVQ створено");

        }

        // Навчання мережі LVQ
        private void TrainLVQNetwork_Click(object sender, EventArgs e)
        {
            if (lvq == null)
            {
                MessageBox.Show("Спочатку створіть мережу або завантажте її з файлу");
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            sWatch2.Reset();
            sWatch2.Start();

            lvq.TrainNetwork();
            sWatch2.Stop();
            Cursor.Current = Cursors.Arrow;
            tSpan = sWatch2.Elapsed;
            this.TimeLVQ.Text = Convert.ToString(tSpan);
            MessageBox.Show("Мережа LVQ навчена");
        }

        // Тестування навчальної вибірки (DeltaBarDelta)
        private void TestTrain_Click(object sender, EventArgs e)
        {
            if (dbd == null)
                MessageBox.Show("Initial error occured");
            else
            {
                ResultTrainDeltaBarDelta = new DataTable();
                this.TrainResults.DataSource = ResultTrainDeltaBarDelta;

                // Створення колонок
                ResultTrainDeltaBarDelta.Columns.Add("id");
                for (int i = 0; i < PARAMETERS; i++)
                {
                    ResultTrainDeltaBarDelta.Columns.Add(ParametersNames[i]);
                }

                ResultTrainDeltaBarDelta.Columns.Add("Cluster");

                for (int i = 0; i < TRAINING_PATTERNS; i++)
                {
                    DataRow row = ResultTrainDeltaBarDelta.NewRow();
                    row[0] = i + 1;
                    for (int k = 0; k < PARAMETERS; k++)
                    {
                        row[k + 1] = inputs[i][k];
                    }
                    row["Cluster"] = dbd.getCluster(inputs[i], output);
                    ResultTrainDeltaBarDelta.Rows.Add(row);
                }
                
            }

        }

        // Тестування навчальної вибірки (LVQ)
        private void TestTrainLVQ_Click(object sender, EventArgs e)
        {
            if (lvq == null)
                MessageBox.Show("Спочатку створіть мережу або завантажте її із файла");
            else
            {
                ResultTrainLVQ = new DataTable();
                this.TrainResultsLVQ.DataSource = ResultTrainLVQ;

                // Створення колонок
                ResultTrainLVQ.Columns.Add("id");
                for (int i = 0; i < PARAMETERS; i++)
                {
                    ResultTrainLVQ.Columns.Add(ParametersNames[i]);
                }

                ResultTrainLVQ.Columns.Add("Кластер");

                for (int i = 0; i < TRAINING_PATTERNS; i++)
                {
                    DataRow row = ResultTrainLVQ.NewRow();
                    row[0] = i + 1;
                    for (int k = 0; k < PARAMETERS; k++)
                    {
                        row[k + 1] = inputs[i][k];
                    }
                    row["Кластер"] = lvq.getCluster(inputs[i]);
                    ResultTrainLVQ.Rows.Add(row);
                }

            }

        }

        // Тестування випадкової вибірки (DeltaBarDelta)
        private void TestRandom_Click(object sender, EventArgs e)
        {
            if (dbd == null)
                MessageBox.Show("Initial error occured");
            else
            {
                ResultTestDeltaBarDelta = new DataTable();
                this.RandomResults.DataSource = ResultTestDeltaBarDelta;

                // Створення колонок
                ResultTestDeltaBarDelta.Columns.Add("id");
                for (int i = 0; i < PARAMETERS; i++)
                {
                    ResultTestDeltaBarDelta.Columns.Add(ParametersNames[i]);
                }

                ResultTestDeltaBarDelta.Columns.Add("Cluster");

                for (int i = 0; i < TestAmount; i++)
                {
                    DataRow row = ResultTestDeltaBarDelta.NewRow();
                    row[0] = i + 1;
                    for (int k = 0; k < PARAMETERS; k++)
                    {
                        row[k + 1] = testArray[i][k];
                    }
                    row["Cluster"] = dbd.getCluster(testArray[i], output);
                    ResultTestDeltaBarDelta.Rows.Add(row);
                }
            }
        }

        // Тестування випадкової вибірки (LVQ)
        private void TestGeneratedLVQ_Click(object sender, EventArgs e)
        {
            if (lvq == null)
                MessageBox.Show("Спочатку створіть мережу або завантажте її із файла");
            else
            {
                ResultTestLVQ = new DataTable();
                this.GeneratedResultsLVQ.DataSource = ResultTestLVQ;

                // Створення колонок
                ResultTestLVQ.Columns.Add("id");
                for (int i = 0; i < PARAMETERS; i++)
                {
                    ResultTestLVQ.Columns.Add(ParametersNames[i]);
                }

                ResultTestLVQ.Columns.Add("Cluster");

                for (int i = 0; i < TestAmount; i++)
                {
                    DataRow row = ResultTestLVQ.NewRow();
                    row[0] = i + 1;
                    for (int k = 0; k < PARAMETERS; k++)
                    {
                        row[k + 1] = testArray[i][k];
                    }
                    row["Cluster"] = lvq.getCluster(testArray[i]);
                    ResultTestLVQ.Rows.Add(row);
                }
            }
        }

        // Запис результатів для навчальної вибірки (DeltaBarDelta)
        private void SaveTrain_Click(object sender, EventArgs e)
        {
            if (PARAMETERS == 0)
            {
                MessageBox.Show("Initial error occured");
                return;
            }
            saveResultsFileDialog.ShowDialog();
        }

        // виклик діалогу для збереження результатів у файл (TrainResults, DeltaBarDelta)
        private void saveResultsFileDialog_FileOk(object sender, CancelEventArgs e)
        {
           
            try
            {
                string filepath = saveResultsFileDialog.FileName;
                string text = "ID".PadRight(10);
                for(int i = 0; i < ParametersNames.Length; i++)
                {
                    text += ParametersNames[i].PadRight(10);
                }
                text += "Cluster".PadRight(10);
                text += Environment.NewLine;
                foreach (DataGridViewRow row in TrainResults.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        text += cell.Value.ToString().PadRight(10);
                       
                    }
                    text += Environment.NewLine + Environment.NewLine;
                }
                File.AppendAllText(filepath, text);
                MessageBox.Show("The results of testing the trainig set have been saved");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // Запис результатів для навчальної вибірки (LVQ)
        private void SaveTrainLVQ_Click(object sender, EventArgs e)
        {
            if (PARAMETERS == 0)
            {
                MessageBox.Show("Initial error occured");
                return;
            }
            saveFileDialog3.ShowDialog();
        }

        // виклик діалогу для збереження результатів у файл (TrainResults, LVQ)
        private void saveFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string filepath = saveFileDialog3.FileName;
                string text = "ID".PadRight(10);
                for (int i = 0; i < ParametersNames.Length; i++)
                {
                    text += ParametersNames[i].PadRight(10);
                }
                text += "Кластер".PadRight(10);
                text += Environment.NewLine;
                foreach (DataGridViewRow row in TrainResultsLVQ.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        text += cell.Value.ToString().PadRight(10);

                    }
                    text += Environment.NewLine + Environment.NewLine;
                }
                File.AppendAllText(filepath, text);
                MessageBox.Show("Результати навчальної вибірки збережено");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // Запис результатів для випадкової вибірки (BackPropagation)
        private void SaveRandom_Click(object sender, EventArgs e)
        {
            if(PARAMETERS == 0)
            {
                MessageBox.Show("Спочатку завантажте дані");
                return;
            }
            saveFileDialog1.ShowDialog();
        }

        // виклик діалогу для збереження результатів у файл (RandomResults, BackPropagation)
        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string filepath = saveFileDialog1.FileName;
                string text = "ID".PadRight(10);
                for (int i = 0; i < ParametersNames.Length; i++)
                {
                    text += ParametersNames[i].PadRight(10);
                }
                text += "Кластер".PadRight(10);
                text += Environment.NewLine;
                foreach (DataGridViewRow row in RandomResults.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        text += Convert.ToString(Math.Round(Convert.ToDouble(cell.Value.ToString()), 3)).PadRight(10);

                    }
                    text += Environment.NewLine + Environment.NewLine;
                }
                File.AppendAllText(filepath, text);
                MessageBox.Show("Результати випадкової вибірки збережено");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // Запис результатів для випадкової вибірки (LVQ)
        private void SaveGeneratedLVQ_Click(object sender, EventArgs e)
        {
            if (PARAMETERS == 0)
            {
                MessageBox.Show("Спочатку завантажте дані");
                return;
            }
            saveFileDialog4.ShowDialog();
        }

        // виклик діалогу для збереження результатів у файл (GeneratedResults, LVQ)
        private void saveFileDialog4_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string filepath = saveFileDialog4.FileName;
                string text = "ID".PadRight(10);
                for (int i = 0; i < ParametersNames.Length; i++)
                {
                    text += ParametersNames[i].PadRight(10);
                }
                text += "Кластер".PadRight(10);
                text += Environment.NewLine;
                foreach (DataGridViewRow row in GeneratedResultsLVQ.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        text += Convert.ToString(Math.Round(Convert.ToDouble(cell.Value.ToString()), 3)).PadRight(10);

                    }
                    text += Environment.NewLine + Environment.NewLine;
                }
                File.AppendAllText(filepath, text);
                MessageBox.Show("Результати випадкової вибірки збережено");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // збереження мережі DeltaBarDelta
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            saveFileDialog.ShowDialog();
        }

        // збереження мережі LVQ
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            saveFileDialog2.ShowDialog();
        }


        // empty method
        private void DownLoadLVQ_Click(object sender, EventArgs e)
        {

        }
    }
}
