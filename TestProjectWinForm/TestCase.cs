/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestProjectWinForm
{
    class TestCase
    {
    }
}
 */
using System.Collections.Generic;

namespace TestProjectWinForm
{
    public class TestCaseRoot
    {
        public List<TestCase> TestCases { get; set; }
    }

    public class TestCase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TestStep> Steps { get; set; }
    }

    public class TestStep
    {
        public string Action { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public int ControlId { get; set; }  //用於wm_command
        public int OffsetX { get; set; }    //用於模擬鼠標點擊x坐標的clock_at
        public int OffsetY { get; set; }    //用於模擬鼠標點擊y坐標的clock_at
    }
}

