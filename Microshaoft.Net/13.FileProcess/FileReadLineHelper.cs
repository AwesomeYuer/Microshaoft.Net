namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    public static class FileReadLinesHelper
    {
        public static  int FileReadLines
                            (
                                string fileName
                                , Func
                                    <
                                        int             //物理行号
                                        , string        //行内容
                                        , bool          //是否中断
                                    > processLineFunc
                            )
        {
            int i = 1;
            using (Stream stream = File.OpenRead(fileName))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string r = string.Empty;
                    while (!sr.EndOfStream)
                    {
                        r = sr.ReadLine();
                        if (processLineFunc != null)
                        {
                            if (processLineFunc(i, r))
                            {
                                break;
                            }
                        }
                        i++;
                    }
                }
            }
            return i;
        }
    }
}
