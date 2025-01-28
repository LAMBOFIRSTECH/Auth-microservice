using System.Text;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Authentifications.Tests;
public static class FakeSessionExtensions
{public static void SetSession(this DefaultHttpContext httpContext)
{
    var session = new Mock<ISession>();
    var sessionData = new Dictionary<string, byte[]>();

    // Simuler la méthode Set pour la session
    session.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
        .Callback<string, byte[]>((key, value) => sessionData[key] = value);

    // Simuler la méthode TryGetValue pour récupérer des valeurs
    session.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
        .Returns((string key, out byte[] value) =>
        {
            if (sessionData.TryGetValue(key, out value))
            {
                return true;
            }

            value = null;
            return false;
        });


    // Méthode SetString directement sur la session
    session.Setup(s => s.SetString(It.IsAny<string>(), It.IsAny<string>()))
        .Callback((string key, string value) =>
        {
            sessionData[key] = Encoding.UTF8.GetBytes(value);
        });

    // Associer la session mockée au httpContext
    httpContext.Session = session.Object;
}

}

