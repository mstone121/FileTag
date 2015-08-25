using System;

namespace TagReader
{
    class TagReader
    {
        public static void Main()
        {
            File test = new File(@"E:\Music\Beck\Guero\Girl.mp3");
            System.Console.Write(test.tag.ToString());
        }

    }
}
