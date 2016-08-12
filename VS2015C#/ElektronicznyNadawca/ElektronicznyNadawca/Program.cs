using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElektronicznyNadawca
{
    class Program
    {
        static void Main(string[] args)
        {
            EN client = new EN();
            client.setServiceAddress(new Uri("https://en-testwebapi.poczta-polska.pl/websrv/labs.php"));
            client.setWSDLAddress(new Uri("https://en-testwebapi.poczta-polska.pl/websrv/labs.wsdl"));
            client.setUserPassword(null, null);

            String retval = "";
            ENLabs.errorType[] errors = null;
            try
            {
                retval = client.hello("Andrzej");
                System.Console.WriteLine(retval);
                // -----------------------------------------------------------------------------
                ENLabs.buforType bufor;
                List<string> guidy;
                // -----------------------------------------------------------------------------
                ENLabs.profilType[] profiles = TestTools.pobierzProfileZalogowanegoUzytkownika(client);
                ENLabs.kartaType[] karty = TestTools.pobierzKartyZalogowanegoUzytkownika(client);
                ENLabs.urzadNadaniaFullType[] urzedyNadania = TestTools.pobierzUrzedyNadaniaDlaKarty(client, karty.First());
                // -----------------------------------------------------------------------------
                TestTools.ustawBiezacaKarte(client, karty.First());
                // -----------------------------------------------------------------------------
                TestTools.utworzNowyBufor(client, out errors, profiles, urzedyNadania, out bufor);
                // -----------------------------------------------------------------------------
                guidy = TestTools.utworzPrzesylki(client, bufor);
                // -----------------------------------------------------------------------------
                TestTools.pobierzEtykietyZBuforu(client, bufor, guidy, out errors);
                // -----------------------------------------------------------------------------
                int idEnvelope = client.sendEnvelope(bufor, out errors);
                // -----------------------------------------------------------------------------
                TestTools.pobierzEtykietyZKoperty(client, idEnvelope ,out errors);
                // -----------------------------------------------------------------------------
                TestTools.pobierzPojedynczeEtykietyZKoperty(client, idEnvelope, out errors);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                if (errors != null)
                {
                    foreach (ENLabs.errorType error in errors)
                    {
                        System.Console.WriteLine("[" + error.errorNumber + "] " + error.errorDesc);
                    }
                }
            }
            System.Console.WriteLine("KONIEC");
            System.Console.ReadKey();
        }

    }
}
