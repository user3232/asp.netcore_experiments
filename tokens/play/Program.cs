using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

// https://dotnetcoretutorials.com/2020/01/15/creating-and-validating-jwt-tokens-in-asp-net-core/


namespace play
{
  class Program
  {
    static void Main(string[] args)
    {
      
      PrintBase64ConversionExperiments();
      PrintJwtStructure();
      PrintDiffrentInstancesOfJwtSecurityToken();
      PrintJwtAesGcmEncryptionDecryption();

      

    }


    # region JwtSecurityToken Experiments
    

    public static void PrintDiffrentInstancesOfJwtSecurityToken() {

      Console.WriteLine();
      Console.WriteLine("**********************************");
      Console.WriteLine("**  JwtSecurityToken experiments  ");
      Console.WriteLine("**********************************");

      Console.WriteLine();
      Console.WriteLine("What SymmetricSecurityKey has..?:");
      var key = "my great secret AA 9!";
      var keyPhraseBytes = StringToBytes(key);
      var symKey = new SymmetricSecurityKey(keyPhraseBytes);
      Console.WriteLine(BytesToString(symKey.Key));
      Console.WriteLine(Base64UrlEncoder.Encode(key));
      Console.WriteLine(ToPrettyJsonString(symKey));
      
      var jwt0 = GenerateJWT1("kiti ksK90AA%$@#", "Me");
      var jwt1 = GenerateJWT2("kiti ksK90AA%$@#", "Me", "HS256", "64");
      var jwt2 = GenerateJWT2("kiti ksK90AA%$@#", "Me", "HS256", "my_ass!!!");
      var jwt3 = GenerateJWT2("kiti ksK90AA%$@#", "Me", "HS512", "my_ass!!!");
      Console.WriteLine(jwt0);
      // "gUIZBSQrozSMU1BqVlEtIVhq_frhM9NyyWK9S-LSI3I"
    }

    

    public static JwtSecurityToken GenerateJWT()
    {
      
      var th = new JwtSecurityTokenHandler();
      // var tokenDescr = new SecurityTokenDescriptor();
      // var token = th.CreateToken(tokenDescr);
      var key = Encoding.UTF8.GetBytes("ksadjfsldkafj;lskdjf;laksjdf;lkasjdfgui");
      // th.CreateEncodedJwt()
      var t = th.CreateJwtSecurityToken(
        issuer: "Me",
        // The audience of a token is the intended recipient of the token
        // e.g. server identifier having protected resource
        audience: "Me",
        subject: new ClaimsIdentity(new []{
          new Claim(ClaimTypes.Name, ClaimValueTypes.String),
          new Claim(ClaimTypes.Email, ClaimValueTypes.Email),
          new Claim("aud", "Aud1"), new Claim("aud", "Aud2")
        }),
        notBefore: DateTime.Now,
        expires: DateTime.Now + TimeSpan.FromMinutes(20),
        issuedAt: DateTime.Now,
        signingCredentials: new SigningCredentials(
          // new System.Security.Cryptography.X509Certificates.X509Certificate2("mycert.crt")
          key: new SymmetricSecurityKey(key),
          algorithm: SecurityAlgorithms.HmacSha256Signature,
          digest: "Sha256"
        ),
        encryptingCredentials: null,
        // new EncryptingCredentials(
        //   key: new SymmetricSecurityKey(key),
        //   alg: SecurityAlgorithms.HmacSha256Signature,
        //   enc: "Sha256"
        // ),
        claimCollection: new Dictionary<string,object>() {
          {"yoyo",3}
        }
      );

      return (JwtSecurityToken) t;

    }

    public static string ToPrettyJsonString(object any) => 
      JsonSerializer.Serialize( any, new JsonSerializerOptions(){WriteIndented = true} );

    public static JwtSecurityToken GenerateJWT1(string keyPhrase, string userId)
    {
      // generate token that is valid for 7 days
      var tokenHandler = new JwtSecurityTokenHandler();
      var keyPhraseBytes = Encoding.UTF8.GetBytes(keyPhrase);
      var signingCredentials = new SigningCredentials(
        new SymmetricSecurityKey(keyPhraseBytes), 
        SecurityAlgorithms.HmacSha256Signature
      );

      // JWE header alg indicating a shared symmetric key is directly used as CEK.
      // "Content Encryption Key (CEK)"
      // JwtConstants.DirectKeyUseAlg == "dir";

      // var signingCredentials = new SigningCredentials(
      //   key: new SymmetricSecurityKey(keyPhraseBytes), 
      //   algorithm: "HS256"//, // signing method: e.g. symetric HS256 (HmacSha256)
      //   // digest: "HS512"     // hashing method e.g. SHA256
      // );
      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(new[] { new Claim("id", userId) }),
        Expires = DateTime.FromBinary(-8585992369287856198).AddDays(7),
        // Expires = DateTime.UtcNow.AddDays(7),
        SigningCredentials = signingCredentials
      };
      var token = tokenHandler.CreateToken(tokenDescriptor);

      return (JwtSecurityToken) token;
    }


    public static JwtSecurityToken GenerateJWT2
    (
      string keyPhrase, 
      string userId,
      string algorithm,
      string digest
    )
    {
      // generate token that is valid for 7 days
      var tokenHandler = new JwtSecurityTokenHandler();
      var keyPhraseBytes = Encoding.UTF8.GetBytes(keyPhrase);
      
      var signingCredentials = new SigningCredentials(
        key: new SymmetricSecurityKey(keyPhraseBytes), 
        algorithm: algorithm, // signature algorithm
        digest: digest     // digest algorithm
      );
      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(new[] { new Claim("id", userId) }),
        Expires = DateTime.FromBinary(-8585992369287856198).AddDays(7),
        // Expires = DateTime.UtcNow.AddDays(7),
        SigningCredentials = signingCredentials
      };
      var token = tokenHandler.CreateToken(tokenDescriptor);

      return (JwtSecurityToken) token;
    }

    # endregion


    # region Conversions Base64 <--> String

    public static string ASCIIStrignToBase64String(string input) => 
      Convert.ToBase64String(Encoding.ASCII.GetBytes(input));


    public static string Base64StringToString(string base64String) =>
      BytesToString(Convert.FromBase64String(base64String));


    public static string StrignToBase64String(string input) => 
      Convert.ToBase64String(StringToBytes(input));
    

    public static byte[] StringToBytes(string s) =>  Encoding.UTF8.GetBytes(s);
    public static string BytesToString(byte[] bs) => Encoding.UTF8.GetString(bs);


    public static void PrintBase64ConversionExperiments(
      string jwt = jwt_ex_header, 
      string jwt_decoded = jwt_ex_header_decoded
    ) 
    {

      Console.WriteLine();
      Console.WriteLine("**********************************");
      Console.WriteLine("**      Base64 Conversions        ");
      Console.WriteLine("**********************************");


      Console.WriteLine();
      Console.WriteLine("JWT:");
      Console.WriteLine(jwt);

      Console.WriteLine();
      Console.WriteLine("JWT as JSON");
      Console.WriteLine(jwt_decoded);

      Console.WriteLine();
      Console.WriteLine("JWT in JSON encoded (ASCII) to Base64");
      Console.WriteLine(ASCIIStrignToBase64String(jwt_decoded));

      Console.WriteLine();
      Console.WriteLine("JWT in JSON encoded (UTF8) to Base64");
      Console.WriteLine(StrignToBase64String(jwt_decoded));

      Console.WriteLine();
      Console.WriteLine("JWT in JSON encoded to Base64Url");
      Console.WriteLine(Base64UrlEncoder.Encode(jwt_decoded));

      Console.WriteLine();
      Console.WriteLine("JWT decoded (Base64UrlEncoder):");
      Console.WriteLine(Base64UrlEncoder.Decode(jwt));

      Console.WriteLine();
      Console.WriteLine("JWT decoded->encoded (Base64UrlEncoder):");
      Console.WriteLine(Base64UrlEncoder.Encode(Base64UrlEncoder.Decode(jwt)));
      

      Console.WriteLine();
      Console.WriteLine("*********************************");
      Console.WriteLine("********* CONCLUSION ************");
      Console.WriteLine("For JWT use Base64UrlEncoder !!!!");
      Console.WriteLine("*********************************");
      Console.WriteLine();
    }

    public static void ExplainCharsAsByteValues(string cs) {
      cs.Select(c => $"{c} = {Convert.ToByte(c)}");
      Console.WriteLine(String.Join(" | ", cs.Select(c => $"{c} = {Convert.ToByte(c)}")));
    }

    # endregion


    # region JWT example data

    public static void PrintJwtStructure() {

      Console.WriteLine();
      Console.WriteLine("**********************************");
      Console.WriteLine("**       Jwt file structure       ");
      Console.WriteLine("**********************************");


      Console.WriteLine();
      Console.WriteLine("Examplary JWT header:");
      Console.WriteLine(jwt_ex_header);
      Console.WriteLine();
      Console.WriteLine("Examplary JWT header decoded:");
      Console.WriteLine(Base64UrlEncoder.Decode(jwt_ex_header));


      Console.WriteLine();
      Console.WriteLine("Examplary JWT payload decoded:");
      Console.WriteLine(jwt_ex_payload);
      Console.WriteLine();
      Console.WriteLine("Examplary JWT payload decoded:");
      Console.WriteLine(Base64UrlEncoder.Decode(jwt_ex_payload));
      

      Console.WriteLine();
      Console.WriteLine("Examplary JWT signature decoded:");
      Console.WriteLine(jwt_ex_signature);
      Console.WriteLine();
      Console.WriteLine("Examplary JWT signature decoded:");
      Console.WriteLine(Base64UrlEncoder.Decode(jwt_ex_signature));
    }

    public const string jwt_ex_header = 
@"eyJraWQiOiIxZTlnZGs3IiwiYWxnIjoiUlMyNTYifQ";

    public const string jwt_ex_header_decoded = 
@"{""kid"":""1e9gdk7"",""alg"":""RS256""}";
// @"{""kid"":""1e**9&g=-|+dk7"",""alg"":""RS256""}";


    public const string jwt_ex_payload = 
@"ewogImlz
cyI6ICJodHRwOi8vc2VydmVyLmV4YW1wbGUuY29tIiwKICJzdWIiOiAiMjQ4
Mjg5NzYxMDAxIiwKICJhdWQiOiAiczZCaGRSa3F0MyIsCiAibm9uY2UiOiAi
bi0wUzZfV3pBMk1qIiwKICJleHAiOiAxMzExMjgxOTcwLAogImlhdCI6IDEz
MTEyODA5NzAsCiAibmFtZSI6ICJKYW5lIERvZSIsCiAiZ2l2ZW5fbmFtZSI6
ICJKYW5lIiwKICJmYW1pbHlfbmFtZSI6ICJEb2UiLAogImdlbmRlciI6ICJm
ZW1hbGUiLAogImJpcnRoZGF0ZSI6ICIwMDAwLTEwLTMxIiwKICJlbWFpbCI6
ICJqYW5lZG9lQGV4YW1wbGUuY29tIiwKICJwaWN0dXJlIjogImh0dHA6Ly9l
eGFtcGxlLmNvbS9qYW5lZG9lL21lLmpwZyIKfQ";

    public const string jwt_ex_signature = 
@"rHQjEmBqn9Jre0OLykYNn
spA10Qql2rvx4FsD00jwlB0Sym4NzpgvPKsDjn_wMkHxcp6CilPcoKrWHcip
R2iAjzLvDNAReF97zoJqq880ZD1bwY82JDauCXELVR9O6_B0w3K-E7yM2mac
AAgNCUwtik6SjoSUZRcf-O5lygIyLENx882p6MtmwaL1hd6qn5RZOQ0TLrOY
u0532g9Exxcm-ChymrB4xLykpDj3lUivJt63eEGGN6DH5K6o33TcxkIjNrCD
4XB1CKKumZvCedgHHF3IAK4dVEDSUoGlH9z4pP_eWYNXvqQOjGs-rDaQzUHl
6cQQWNiDpWOl_lxXjQEvQ";

    # endregion


    # region AEAD AesGcm 

    public static (string packageJwt, string keyBase64, string nonceBase64) AesGcmEncrypt(
      string key = "secret key is ok but 32B is must",
      //           "                                " <- 32 bytes
      string message = "some text to encrypt using AES GCM"
    ) {
      byte[] keyBytes = 
        Encoding.UTF8.GetBytes(key);
      byte[] nonceBytes = new byte[]{ // AES GCM requires 12 bytes
        1,2,3,4,5,6,7,8,9,10,11,12    // The security guarantees of the
      };                              // AES-GCM algorithm mode require
                                      // that the same nonce value is
                                      // never used twice with the same key.
      // new Random((int) DateTime.Now.TimeOfDay.Ticks)
      //   .NextBytes(nonceBytes);                      // random nonce
      byte[] plaintextBytes = 
        Encoding.UTF8.GetBytes(message);
      byte[] ciphertextBytes = 
        new byte[plaintextBytes.Length];
      byte[] tagBytes = new byte[16];     // 12, 13, 14, 15, or 16 bytes 
                                          // (96, 104, 112, 120, or 128 bits).
      var assocData = "{\"alg\"=\"AesGcm\"}";
      var assocDataBytes = Encoding.UTF8.GetBytes(assocData);
      // var associatedData = JsonSerializer
      //   .SerializeToUtf8Bytes(
      //     new Dictionary<string,string>(){
      //       {"alg", "AesGcm"} 
      //     }
      //     // , new JsonSerializerOptions(){WriteIndented = true} // pretty print
      //   );
      AesGcm aes_gcm_encrypting = new AesGcm(
        keyBytes                      // must be: 16, 24, or 32 bytes 
                                      // (128, 192, or 256 bits)
      );
      aes_gcm_encrypting.Encrypt(
        nonce: nonceBytes,            // must be: 12 bytes (96 bits)
                                      // The nonce associated with this message, 
                                      // which should be a unique value for every
                                      // operation with the same key.
        plaintext: plaintextBytes,    // message to encrypt
        ciphertext: ciphertextBytes,  // byte array wher write encrypted message
        tag: tagBytes,                // byte array where write message MAC
                                      // must be: 12, 13, 14, 15, or 16 bytes 
                                      // (96, 104, 112, 120, or 128 bits).
        associatedData: assocDataBytes// data not encrypted but probably signed
                                      // identical must be during decryption
                                      // or decryption will fail
      );

      var packageJwt = 
        Base64UrlEncoder.Encode(assocDataBytes) +  // header
        "." +                                      // .
        Base64UrlEncoder.Encode(ciphertextBytes) + // payload
        "." +                                      // .
        Base64UrlEncoder.Encode(tagBytes);         // signature


      return (
        packageJwt: packageJwt, 
        keyBase64: Base64UrlEncoder.Encode(key), 
        nonceBase64: Base64UrlEncoder.Encode(nonceBytes)
      );
    }

    public static (string message, string associatedData) AesGcmDecrypt(
      string packageJwt,
      string keyBase64,
      string nonceBase64
    ) {

      var parts = Regex.Split(packageJwt, @"\.");
      var headerBytes = Base64UrlEncoder.DecodeBytes(parts[0]);
      var payloadBytes = Base64UrlEncoder.DecodeBytes(parts[1]);
      var signatureBytes = Base64UrlEncoder.DecodeBytes(parts[2]);

      var keyBytes = Base64UrlEncoder.DecodeBytes(keyBase64);
      var nonceBytes = Base64UrlEncoder.DecodeBytes(nonceBase64);

      var plaintextBytes = new byte[payloadBytes.Length];

      AesGcm aes_gcm_decrypting = new AesGcm(keyBytes);
      aes_gcm_decrypting.Decrypt(
        nonce: nonceBytes,
        ciphertext: payloadBytes,
        tag: signatureBytes,
        plaintext: plaintextBytes,
        associatedData: headerBytes
      );

      return (
        message: Encoding.UTF8.GetString(plaintextBytes),
        associatedData: Encoding.UTF8.GetString(headerBytes)
      );
    }

    public static (string message, string associatedData) AesGcmDecrypt(string packageJwt) {
      return AesGcmDecrypt(
        packageJwt: packageJwt, 
        keyBase64: Base64UrlEncoder.Encode(
          "secret key is ok but 32B is must"     // 32B passphrase
        ), 
        nonceBase64: Base64UrlEncoder.Encode(
          new byte[]{1,2,3,4,5,6,7,8,9,10,11,12} // 12B nonce
        ) 
      );
    }


    public static void PrintJwtAesGcmEncryptionDecryption() {

      Console.WriteLine();
      Console.WriteLine("**********************************");
      Console.WriteLine("**       Jwt as AEAD AesGcm       ");
      Console.WriteLine("**********************************");


      Console.WriteLine();
      Console.WriteLine("Generating JWT using AesGcm of message:");
      var message = "some text to encrypt using AES GCM";
      Console.WriteLine(message);
      Console.WriteLine();
      Console.WriteLine("Encrypting ...:");
      (var packageJwt, _ , _ ) = AesGcmEncrypt(
        "secret key is ok but 32B is must",
        message
      );
      Console.WriteLine();
      Console.WriteLine("JWT AES GCM::");
      Console.WriteLine(packageJwt);
      Console.WriteLine();
      Console.WriteLine("Decrypting ...:");
      var decrypted = AesGcmDecrypt(packageJwt);
      Console.WriteLine();
      Console.WriteLine("Decrypted message:");
      Console.WriteLine(decrypted);
    }
    

    # endregion





  }
}
