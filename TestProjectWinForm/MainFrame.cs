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
using FlaUI.Core.AutomationElements.Infrastructure; // AutomationElement（1.3.0 位置）
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;          // ControlType
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

        //鼠標瞬間移動到坐標
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        //設置窗口在頂層
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd,IntPtr hWndInsertAfter,int X, int Y, int cx, int cy,uint uFlags);

        const uint WM_COMMAND = 0x0111;
        const uint WM_KEYDOWN = 0x0100;
        const uint WM_KEYUP = 0x0101;
        const int VK_RETURN = 0x0D;
        const int SW_RESTORE = 9;

        //設置窗口在頂層
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;



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
                //Cleanup();
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
            int timeout = 0;
            switch (step.Action)
            {
                case "LaunchApplication":
                    string appPath = step.Parameters["appPath"];
                    _app = FlaUI.Core.Application.Launch(appPath);
                    AppendLog(string.Format("启动应用: {0}", appPath));
                    break;

                case "WaitForWindow":
                    {
                        // ---------- 1) 参数（安全解析，绝不再抛 FormatException）----------
                        string title = step.Parameters.ContainsKey("title")
                            ? (step.Parameters["title"] ?? "").Trim()
                            : "";

                        timeout = ParseIntParam(step.Parameters, "timeout", 10);
                        if (timeout <= 0) timeout = 10;

                        if (string.IsNullOrEmpty(title))
                            throw new ArgumentException("WaitForWindow 必须提供非空 title 参数");

                        AppendLog(string.Format("WaitForWindow: title='{0}', timeout={1}s", title, timeout));

                        // ---------- 2) 轮询查找窗口 ----------
                        DateTime deadline = DateTime.Now.AddSeconds(timeout);
                        FlaUI.Core.AutomationElements.Infrastructure.AutomationElement found = null;

                        while (DateTime.Now < deadline && found == null)
                        {
                            // 路径①：当前 _app 的顶层窗口（快、同进程）
                            found = FindTopLevelWindowByTitle(title);

                            // 路径②：桌面全局兜底（跨进程也能命中，较慢）
                            if (found == null)
                                found = FindDesktopWindowByTitle(title);

                            if (found == null)
                                System.Threading.Thread.Sleep(300);
                        }

                        if (found == null)
                        {
                            string msg = string.Format("超时 {0}s 未找到标题为 '{1}' 的窗口", timeout, title);
                            AppendLog(msg);
                            throw new Exception(msg);
                        }

                        // ---------- 3) 转 Window、必要时重新 Attach ----------
                        _mainWindow = found.AsWindow();

                        int pid = GetProcessIdSafe(found);
                        if (pid > 0 && (_app == null || _app.HasExited || _app.ProcessId != pid))
                        {
                            _app = FlaUI.Core.Application.Attach(pid);
                            AppendLog(string.Format("已重新 Attach 到进程 {0}", pid));
                        }

                        AppendLog(string.Format("获取窗口成功: '{0}' (pid={1})", SafeName(_mainWindow), pid));
                        break;
                    }


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

                    SetWindowTopmost(_mainWindow, true);   // 点击前置顶
                    System.Threading.Thread.Sleep(100); 

                    var rect = _mainWindow.Properties.BoundingRectangle.ValueOrDefault;
                    int x = (int)rect.Left + step.OffsetX;
                    int y = (int)rect.Top + step.OffsetY;

                    // 直接瞬间把光标放到目标点
                    SetCursorPos(x, y);
                    // 稍等一下，确保光标位置已生效，再点击
                    System.Threading.Thread.Sleep(50);

                    //FlaUI.Core.Input.Mouse.MoveTo(x, y); //可看到鼠標移動軌跡
                    FlaUI.Core.Input.Mouse.Click(FlaUI.Core.Input.MouseButton.Left);
                    AppendLog(string.Format("--> 已点击坐标 ({0},{1})",x,y));

                    SetWindowTopmost(_mainWindow, false);// 点击后取消置顶
                    //點擊后等待200ms,讓窗口有響應時間
                    System.Threading.Thread.Sleep(2000);
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


        //case "WaitForWindow":辅助方法
        private static int ParseIntParam(Dictionary<string, string> p, string key, int defaultValue)
        {
            if (p == null || !p.ContainsKey(key)) return defaultValue;

            string raw = (p[key] ?? "").Trim();
            if (raw.Length == 0) return defaultValue;

            int v;
            if (int.TryParse(raw, out v)) return v;

            // 兼容 YAML 数字被转成 "10.0" 的情况
            double d;
            if (double.TryParse(raw, out d)) return (int)d;

            throw new Exception(string.Format(
                "参数 '{0}' 的值 '{1}' 不是有效整数（原始长度={2}）",
                key, raw, raw.Length));
        }

        private FlaUI.Core.AutomationElements.Infrastructure.AutomationElement
            FindTopLevelWindowByTitle(string title)
        {
            if (_app == null || _app.HasExited) return null;

            try
            {
                foreach (var w in _app.GetAllTopLevelWindows(_automation))
                {
                    try
                    {
                        if (w.Name == title && !w.Properties.IsOffscreen.ValueOrDefault)
                            return w;   // Window → AutomationElement 隐式向上转型
                    }
                    catch { /* 失效元素跳过 */ }
                }
            }
            catch { /* _app 已失效 */ }

            return null;
        }

        private FlaUI.Core.AutomationElements.Infrastructure.AutomationElement
            FindDesktopWindowByTitle(string title)
        {
            try
            {
                var desktop = _automation.GetDesktop();
                return desktop.FindFirstDescendant(cf =>
                    cf.ByName(title).And(cf.ByControlType(ControlType.Window)));
            }
            catch (Exception ex)
            {
                // 桌面查找容易在旧窗口刚关闭时抛 ElementNotAvailable / COM 超时，
                // 只记录、不中断轮询。
                AppendLog("桌面查找异常: " + ex.Message);
                return null;
            }
        }

        private int GetProcessIdSafe(
            FlaUI.Core.AutomationElements.Infrastructure.AutomationElement el)
        {
            try { return el.Properties.ProcessId.ValueOrDefault; }
            catch { return 0; }
        }

        private string SafeName(
            FlaUI.Core.AutomationElements.Infrastructure.AutomationElement el)
        {
            try { return el.Name; }
            catch { return "<unavailable>"; }
        }


        //窗口置頂方法
        private void SetWindowTopmost(Window window, bool topmost)
        {
            if (window == null) return;

            try
            {
                IntPtr handle = window.Properties.NativeWindowHandle.ValueOrDefault;
                if (handle == IntPtr.Zero) return;

                // topmost 为 true 时使用 HWND_TOPMOST，否则使用 HWND_NOTOPMOST ( -2 )
                IntPtr insertAfter = topmost ? new IntPtr(HWND_TOPMOST) : new IntPtr(-2);

                SetWindowPos(
                    handle,
                    insertAfter,
                    0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch (Exception ex)
            {
                AppendLog("设置窗口置顶状态失败: " + ex.Message);
            }
        }

  
     }
}

