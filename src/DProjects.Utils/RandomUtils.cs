
using System;
using System.Security.Cryptography;
using System.Text;

namespace DProjects.Utils {


    public static class RandomUtils {


        //variables
        private static Random mRandom = new Random(DateTime.Now.Millisecond);


        //random number
        public static double GetRandomNumber() {
            return mRandom.NextDouble();
        }
        public static double GetRandomNumber(double min, double max) {
            double d = GetRandomNumber();
            double diff = max - min;
            return min + diff * d;
        }
        public static int GetRandomNumber(int min, int max) {
            return mRandom.Next(min, max);
        }
        public static byte[] GetRandomNumber(int length) {
            using (var generator = System.Security.Cryptography.RandomNumberGenerator.Create()) {
                byte[] data = new byte[length];
                generator.GetBytes(data);
                return data;
            }
        }
        public static string GetRandomNumberHex(int length) {
            var bytes = GetRandomNumber(length);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }


        //random password 
        public static string GenerateRandomPassword(int intPasswordLength, bool allowSymbols) {
            RandomKeyGenerator randomKeyGenerator = new RandomKeyGenerator();
            if (allowSymbols) {
                randomKeyGenerator.KeyLetters = "abcdefghijklmnopqrstuvwxyz$(-=)!?¿";
            }
            randomKeyGenerator.KeyChars = intPasswordLength;
            return randomKeyGenerator.Generate();
        }
        private class RandomKeyGenerator {
            private string mKey_Letters = "abcdefghijklmnopqrstuvwxyz";
            private string mKey_Numbers = "0123456789";
            private int mKey_Chars = 12;
            private char[]? mLettersArray;
            private char[]? mNumbersArray;
            private System.Random mRandom;
            public RandomKeyGenerator() {
                mRandom = new System.Random();
            }
            protected internal string KeyLetters {
                set { mKey_Letters = value; }
            }
            protected internal string KeyNumbers {
                set { mKey_Numbers = value; }
            }
            protected internal int KeyChars {
                set { mKey_Chars = value; }
            }
            public string Generate() {
                int i_key = 0;
                double Random1 = 0;
                short arrIndex = 0;
                var sb = new StringBuilder();
                string RandomLetter = "";
                mLettersArray = mKey_Letters.ToCharArray();
                mNumbersArray = mKey_Numbers.ToCharArray();
                for (i_key = 1; i_key <= mKey_Chars; i_key++) {
                    Random1 = mRandom.NextDouble();
                    arrIndex = (short)(-1);
                    if ((System.Convert.ToInt32(Random1 * 111)) % 2 == 0) {
                        while (arrIndex < 0) {
                            arrIndex = Convert.ToInt16(mLettersArray.GetUpperBound(0) * Random1);
                        }
                        RandomLetter = mLettersArray[arrIndex].ToString();
                        if ((System.Convert.ToInt32(arrIndex * Random1 * 99)) % 2 != 0) {
                            RandomLetter = mLettersArray[arrIndex].ToString();
                            RandomLetter = RandomLetter.ToUpper();
                        }
                        sb.Append(RandomLetter);
                    } else {
                        while (arrIndex < 0) {
                            arrIndex = Convert.ToInt16(mNumbersArray.GetUpperBound(0) * Random1);
                        }
                        sb.Append(mNumbersArray[arrIndex]);
                    }
                }
                return sb.ToString();
            }
        }


        //random key
        public static byte[] GenerateRandomKey(int length) {
            var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            byte[] randomBytes = new byte[length];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return randomBytes;
        }
        public static string GenerateRandomKeyHex(int length) {
            return HexUtils.Hex(GenerateRandomKey(length));
        }
        public static string GenerateRandomKeyBase64(int length) {
            return Convert.ToBase64String(GenerateRandomKey(length));
        }
        public static byte[] GenerateSalt(int length) {
            byte[] result = new byte[length];
            using (var keyGenerator = RandomNumberGenerator.Create()) {
                keyGenerator.GetBytes(result);
            }
            return result;
        }
    }


}


