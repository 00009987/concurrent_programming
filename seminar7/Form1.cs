using CP_MockTest_DLL;
using System.Text;

namespace MockForm
{
    public partial class Form1 : Form
    {
        FileServer fs = CP_MockTest_DLL.FileServer.GetInstance();
        SemaphoreSlim semaphore = new SemaphoreSlim(4, 4);
        ReaderWriterLock rwLock = new ReaderWriterLock();
        CountdownEvent countdownEvent = new CountdownEvent(10);

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            init();
        }

        private void init()
        {
            btnStart.Enabled = false;

            for (int i = 0; i < 10; i++)
            {
                Thread thread = new Thread(GetAndSave);
                thread.Start(i);
            }

            new Thread(() =>
            {
                countdownEvent.Wait();

                invoke(() =>
                {
                    btnStart.Enabled = true;
                });

                countdownEvent.Reset();
            }).Start();
        }
        private void GetAndSave(object threadId)
        {
            semaphore.Wait();

            int id = (int) threadId;

            // thread start info
            invoke(() =>
            {
                string message = string.Format($"{DateTime.Now} Thread {id} is starting");
                listBoxThreads.Items.Add(message);
            });

            // file logs

            rwLock.AcquireWriterLock(Timeout.Infinite);

            string filePath = "txt.txt";
            byte[] array = fs.GetFile(id);
            string content = Encoding.Default.GetString(array);
            string existingContent = File.ReadAllText(filePath);

            if (existingContent.Length != 0)
            {
                File.AppendAllText(filePath, content);
            } else
            {
                File.WriteAllBytes(filePath, array);
            }

            rwLock.ReleaseWriterLock();


            invoke(() =>
            {
                string message = string.Format($"Thread {id} {content}");
                listBoxLogs.Items.Add(message);
            });

            // thread finish info
            invoke(() =>
            {
                string message = string.Format($"{DateTime.Now} Thread {id} is finished");
                listBoxThreads.Items.Add(message);
            });

            countdownEvent.Signal();
            semaphore.Release();
        }

        void invoke(Action func)
        {
            Invoke(new MethodInvoker(() =>
            {
                func();
            }));
        }
    }
}