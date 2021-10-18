using System.Security.Cryptography;

namespace ProblemSets.Additional_Methods
{
    public class GetKey
    {
        public static byte[] GenerateKey()
        {
            var key = new byte[128];

            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(key);
            }

            return key;
        }
    }
}