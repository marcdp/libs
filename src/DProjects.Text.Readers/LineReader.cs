using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Text.Readers {


    public class LineReader(TextReader reader, bool leaveOpen = false) : TextReader, IDisposable {


        //variables
        private List<string> mPushedBackLines = [];
        private TextReader mReader = reader;
        private bool mLeaveOpen = leaveOpen;

        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (!mLeaveOpen) {
                    if (mReader != null) {
                        mReader.Dispose();
                    }
                }
            }
        }

        //methods
        public override int Read() {
            return mReader.Read();
        }
        public override async Task<int> ReadAsync(char[] buffer, int index, int count) {
            return await mReader.ReadAsync(buffer, index, count);
        }
        public override int ReadBlock(char[] buffer, int index, int count) { 
            return mReader.ReadBlock(buffer, index, count);
        }
        public override async Task<int> ReadBlockAsync(char[] buffer, int index, int count) {
            return await mReader.ReadBlockAsync(buffer, index, count);
        }
        public override string ReadToEnd() {
            var sb = new StringBuilder();
            do {
                var line = mReader.ReadLine();
                if (line == null) break;
                sb.AppendLine(line);
            } while (true);
            return sb.ToString();
        }
        public override async Task<string> ReadToEndAsync() {
            var sb = new StringBuilder();
            do {
                var line = await mReader.ReadLineAsync();
                if (line == null) break;
                sb.AppendLine(line);
            } while (true);
            return sb.ToString();
        }
        public override string? ReadLine() {
            if (mPushedBackLines.Count > 0) {
                var line = mPushedBackLines[0];
                mPushedBackLines.RemoveAt(0);
                return line;
            }
            return mReader.ReadLine();
        }
        public override async Task<string?> ReadLineAsync() {
            if (mPushedBackLines.Count > 0) {
                var line = mPushedBackLines[0];
                mPushedBackLines.RemoveAt(0);
                return line;
            }
            return await mReader.ReadLineAsync();
        }
        public virtual void PushBackLine(string line) {
            mPushedBackLines.Add(line);
        }


    }


}
