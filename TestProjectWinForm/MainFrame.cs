using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


using System.Runtime.InteropServices; //用於與非託管代碼(win 32 API,Com組件)交互,Dll導入,Marshal類,StructLayoutAttribute,ComImportAttribute等
using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA2;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using FlaUI.Core.Tools;
using System.Threading;

namespace TestProjectWinForm
{
    public partial class AutoTestTool : Form
    {
        // 存储当前加载的测试用例
        private List<TestCase> _testCases = new List<TestCase>();
        // 当前加载的 YAML 文件路径
        private string _yamlFilePath;

        // FlaUI 相关对象（在测试执行时创建）
        private FlaUI.UIA2.UIA2Automation _automation;
        private FlaUI.Core.Application _app;
        private FlaUI.Core.AutomationElements.Window _mainWindow;


        //-------------Win32 API-------------
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const uint WM_COMMAND = 0x0111;
        const uint WM_KEYDOWN = 0x0100;
        const uint WM_KEYUP = 0x0101;
        const int VK_RETURN = 0x0D;
        const int SW_RESTORE = 9;


        //構造函數,用於初始化
        public AutoTestTool()
        {
            InitializeComponent();

            // 初始禁用执行按钮
            btnExecute.Enabled = false;
            btnExecuteAll.Enabled = false;
        }

        #region 事件处理

        // 打开用例文件
        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "YAML 文件|*.yaml;*.yml|所有文件|*.*";
                ofd.Title = "选择测试用例文件";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _yamlFilePath = ofd.FileName;
                    LoadTestCases(_yamlFilePath);
                }
            }
        }

        // 执行选中的用例
        private void btnExecute_Click(object sender, EventArgs e)
        {
            if (lstTestCases.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个测试用例", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedName = lstTestCases.SelectedItem.ToString();
            var testCase = _testCases.FirstOrDefault(tc => tc.Name == selectedName);
            if (testCase != null)
            {
                ExecuteTestCase(testCase);
            }
        }

        // 执行所有用例
        private void btnExecuteAll_Click(object sender, EventArgs e)
        {
            if (_testCases.Count == 0)
            {
                MessageBox.Show("没有加载任何测试用例", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var tc in _testCases)
            {
                ExecuteTestCase(tc);
            }
        }

        #endregion

        #region 核心逻辑

        /// <summary>
        /// 加载并解析 YAML 文件，填充列表
        /// </summary>
        private void LoadTestCases(string filePath)
        {
            try
            {
                string yamlContent = File.ReadAllText(filePath);
                var deserializer = new Deserializer();


                TestCaseRoot root = null;
                using (var reader = new StringReader(yamlContent))
                {
                    // Deserialize 方法接受 TextReader
                    root = deserializer.Deserialize<TestCaseRoot>(reader);
                }

                //var root = deserializer.Deserialize<TestCaseRoot>(yamlContent);

                if ((root != null?root.TestCases:null) == null || root.TestCases.Count == 0)
                {
                    MessageBox.Show("YAML 文件中没有找到任何测试用例", "解析错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _testCases = root.TestCases;
                // 刷新 ListBox
                lstTestCases.Items.Clear();
                foreach (var tc in _testCases)
                {
                    lstTestCases.Items.Add(tc.Name);
                }

                btnExecute.Enabled = true;
                btnExecuteAll.Enabled = true;
                AppendLog(string.Format("成功加载{0}个测试用例from{1}",_testCases.Count,Path.GetFileName(filePath)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("加载 YAML 文件失败:{0}", ex.Message),"错误",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 执行单个测试用例（在 UI 线程同步执行，会短暂卡顿）
        /// </summary>
        private void ExecuteTestCase(TestCase testCase)
        {
            // 禁用按钮，防止重复点击
            btnExecute.Enabled = false;
            btnExecuteAll.Enabled = false;

            try
            {
                AppendLog(string.Format("=== 开始执行: {0} ===", testCase.Name));
                AppendLog(string.Format("描述: {0}", testCase.Description));

                // 创建 UIA2 自动化实例
                using (_automation = new UIA2Automation())
                {
                    // 依次执行步骤
                    foreach (var step in testCase.Steps)
                    {
                        AppendLog(string.Format("执行步骤: {0}", step.Action));
                        ExecuteStep(step);
                    }
                }

                AppendLog(string.Format("✅ 测试用例 [{0}] 执行成功\n", testCase.Name));
            }
            catch (Exception ex)
            {
                AppendLog(string.Format("❌ 测试用例 [{0}] 执行失败: {1}\n", testCase.Name, ex.Message));
                MessageBox.Show(string.Format("执行失败: {0}",ex.Message),"错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cleanup();
                // 启用按钮
                btnExecute.Enabled = true;
                btnExecuteAll.Enabled = true;
            }
        }

        /// <summary>
        /// 执行单个步骤
        /// </summary>
        private void ExecuteStep(TestStep step)
        {
            switch (step.Action)
            {
                case "LaunchApplication":
                    string appPath = step.Parameters["appPath"];
                    _app = FlaUI.Core.Application.Launch(appPath);
                    AppendLog(string.Format("启动应用: {0}", appPath));
                    break;

                case "WaitForWindow":
                    string title = step.Parameters["title"];
                    int timeout = int.Parse(step.Parameters["timeout"]);
                    _mainWindow = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(timeout));

                    if (_mainWindow != null)
                    {
                        AppendLog(string.Format("获取窗口: {0}", _mainWindow.Title));
                    }
                    else
                    {
                        AppendLog("获取窗口失败: 未找到窗口");
                        throw new Exception(string.Format("未找到标题为 '{0}' 的窗口", title));
                    }


                    //AppendLog(string.Format("获取窗口: {0}", _mainWindow.Title));
                    break;

                    //扎到并進行點擊
                case "FindAndClick":
                    string controlType = step.Parameters["controlType"];
                    string name = step.Parameters["name"];
                    PerformFindAndClick(controlType, name);
                    break;

                    //給指定窗口發送命令
                    // 关键：向主窗口发送 WM_COMMAND，wParam 低位为控件 ID
                case "wm_command":
                    timeout = int.Parse(step.Parameters["timeout"]);
                    _mainWindow = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(timeout));
                    if (_mainWindow == null)
                    {
                        AppendLog(string.Format("未获取到主窗口"));
                        return;
                    }
                    IntPtr hwnd = _mainWindow.Properties.NativeWindowHandle.ValueOrDefault;
                    SendMessage(hwnd, WM_COMMAND, (IntPtr)step.ControlId, IntPtr.Zero);
                    AppendLog(string.Format("--> 已发送WM_COMMAND:{0}, ID:{1}", WM_COMMAND, step.ControlId));
                    break;

                    //發送Enter按鈕按下,當窗口默認按下Enter就是點擊OK/確認鍵時使用
                    // 先让窗口到前台，再发 Enter（MFC 默认按钮会响应）
                case "send_enter":
                    timeout = int.Parse(step.Parameters["timeout"]);
                    _mainWindow = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(timeout));
                    if (_mainWindow == null)
                    {
                        AppendLog(string.Format("未获取到主窗口"));
                        return;
                    }
                    hwnd = _mainWindow.Properties.NativeWindowHandle.ValueOrDefault;

                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);
                    Thread.Sleep(200);
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_RETURN, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP,   (IntPtr)VK_RETURN, IntPtr.Zero);
                    AppendLog(string.Format("--> 已发送 Enter"));
                    break;
                
                    //根據窗口位置來模擬按鈕點擊
                    // 兜底：基于窗口矩形 + 偏移计算坐标后点击
                case "click_at":
                    timeout = int.Parse(step.Parameters["timeout"]);
                    _mainWindow = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(timeout));
                    if (_mainWindow == null)
                    {
                        AppendLog(string.Format("未获取到主窗口"));
                        return;
                    }
                    var rect = _mainWindow.Properties.BoundingRectangle.ValueOrDefault;
                    int x = (int)rect.Left + step.OffsetX;
                    int y = (int)rect.Top + step.OffsetY;
                    FlaUI.Core.Input.Mouse.MoveTo(x, y);
                    FlaUI.Core.Input.Mouse.Click(FlaUI.Core.Input.MouseButton.Left);
                    AppendLog(string.Format("--> 已点击坐标 ({0},{1})",x,y));
                    break;


                default:
                    AppendLog(string.Format("未知操作: {0}",step.Action));
                    break;
            }
        }

        /// <summary>
        /// 查找控件并点击（针对 MFC 使用 ByText）
        /// </summary>
        private void PerformFindAndClick(string controlType, string name)
        {
            if (_mainWindow == null)
                throw new Exception("未获取到主窗口，请先执行 WaitForWindow");

            var cf = new ConditionFactory(new UIA2PropertyLibrary());
            ConditionBase condition;

            switch (controlType.ToLower())
            {
                case "menuitem":
                    condition = cf.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem)
                                  .And(cf.ByText(name));
                    break;
                case "button":
                    condition = cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)
                                  .And(cf.ByText(name));
                    break;
                default:
                    condition = cf.ByText(name);
                    break;
            }

            var element = RetryWhileNull(
                () => _mainWindow.FindFirstDescendant(condition),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromMilliseconds(200)
            );

            //var element = _mainWindow.FindFirstDescendant(condition, TimeSpan.FromSeconds(3));
            if (element == null)
                throw new Exception(string.Format("未找到控件:{0},類型:{1}", name, controlType));

                element.Click();
            //clickable.Click();
                AppendLog(string.Format("点击控件: {0} ({1})", name,controlType));
        }

        public static T RetryWhileNull<T>(Func<T> action, TimeSpan timeout, TimeSpan retryInterval) where T : class
        {
            var startTime = DateTime.Now;
            T result = null;

            while ((DateTime.Now - startTime) < timeout)
            {
                result = action();
                if (result != null)
                {
                    break;
                }
                Thread.Sleep(retryInterval);
            }

            return result;
        }



        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            try
            {
                if (_app != null && !_app.HasExited)
                {
                    _app.Close();
                    AppendLog("已关闭应用");
                }
            }
            catch { /* 忽略 */ }
            finally
            {
                _app = null;
                _mainWindow = null;
                if (_automation != null)
                    {
                     _automation.Dispose();
                    }
                //_automation?.Dispose();
                _automation = null;
            }
        }

        /// <summary>
        /// 向日志 RichTextBox 追加文本（线程安全）
        /// </summary>
        private void AppendLog(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() => AppendLog(message)));
                return;
            }
            rtbLog.AppendText(message + Environment.NewLine);
            // 滚动到底部
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
        }

        #endregion

        // 窗体设计器自动生成的代码（略，下面给出关键部分）
        // 你需要将控件拖到窗体上，并关联事件。







        private void AutoTestTool_Load(object sender, EventArgs e)
        {

        }
  
     }
}

