using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Text.Readers {


    public class XmlDocumentsReader : IDisposable {
        //read multiple xml documents in a stream

        //inner class
        private class MyXmlReader : XmlTextReader {
            private TextReader mTextReader;
            private int mDeep;
            private bool mEof;
            public MyXmlReader(TextReader textReader) : base(textReader) {
                mTextReader = textReader;
                mDeep = 0;
            }
            public override bool Read() {
                if (!mEof && base.Read()) {
                    if (NodeType == XmlNodeType.Element) {
                        mDeep++;
                    } else if (NodeType == XmlNodeType.EndElement) {
                        if (--mDeep == 0) {
                            mEof = true;
                        }
                    }
                    return true;
                }
                return false;
            }
            public override async Task<bool> ReadAsync() {
                if (!mEof && base.Read()) {
                    if (NodeType == XmlNodeType.Element) {
                        mDeep++;
                    } else if (NodeType == XmlNodeType.EndElement) {
                        if (--mDeep == 0) {
                            mEof = true;
                        }
                    }
                    return true;
                }
                return false;
            }
        }


        //variables
        private MyXmlReader mMyXmlReader;
        private bool mLeaveOpen;
        private bool mEof;


        //constructor
        public XmlDocumentsReader(TextReader textReader, bool leaveOpen = false) {
            mMyXmlReader = new MyXmlReader(textReader);
            mLeaveOpen = leaveOpen;
        }
        public void Dispose() {
            if (!mLeaveOpen) {
                mMyXmlReader.Dispose();
            }
        }


        //methods
        public XmlDocument? Read() {
            if (mEof) return null;
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(mMyXmlReader);
            var remainder = mMyXmlReader.GetRemainder();
            if (remainder.Peek() == -1) mEof = true;
            mMyXmlReader = new MyXmlReader(remainder);
            return xmlDocument;
        }
        public async Task<XmlDocument?> ReadAsync(CancellationToken cancellationToken) {
            if (mEof) return null;
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(mMyXmlReader);
            var remainder = mMyXmlReader.GetRemainder();
            if (remainder.Peek() == -1) mEof = true;
            mMyXmlReader = new MyXmlReader(remainder);
            return xmlDocument;
        }
    }


}

