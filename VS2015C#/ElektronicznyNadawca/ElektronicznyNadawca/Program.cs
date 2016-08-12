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
                ENLabs.profilType[] profiles = pobierzProfileZalogowanegoUzytkownika(client);
                ENLabs.kartaType[] karty = pobierzKartyZalogowanegoUzytkownika(client);
                ENLabs.urzadNadaniaFullType[] urzedyNadania = pobierzUrzedyNadaniaDlaKarty(client, karty.First());
                // -----------------------------------------------------------------------------
                ustawBiezacaKarte(client, karty.First());
                // -----------------------------------------------------------------------------
                ENLabs.buforType bufor;
                utworzNowyBufor(client, out errors, profiles, urzedyNadania, out bufor);
                // -----------------------------------------------------------------------------
                List<string> guidy = utworzPrzesylki(client, bufor);
                // -----------------------------------------------------------------------------
                errors = pobierzEtykietyZBuforu(client, bufor, guidy);

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

        private static void utworzNowyBufor(EN client, out ENLabs.errorType[] errors, ENLabs.profilType[] profiles, ENLabs.urzadNadaniaFullType[] urzedyNadania, out ENLabs.buforType bufor)
        {
            bufor = zdefiniujBufor(profiles.First(), urzedyNadania.First());
            errors = client.createEnvelopeBufor(bufor);
            System.Console.WriteLine("Utworzno bufor " + bufor.idBufor);
        }

        private static List<string> utworzPrzesylki(EN client, ENLabs.buforType bufor)
        {
            System.Console.WriteLine("-----Utworzenie przesyłek");
            List<ENLabs.przesylkaType> przesylki = new List<ENLabs.przesylkaType>();
            przesylki.Add(createPocztexUiszczaOplateNadawca());
            przesylki.Add(createPocztexBledna());
            List<string> guidy = nadajPrzesylki(client, bufor, przesylki);
            return guidy;
        }

        private static List<string> nadajPrzesylki(EN client, ENLabs.buforType bufor, List<ENLabs.przesylkaType> przesylki)
        {
            ENLabs.addShipmentResponseItemType[] przesylkiWyslane = client.addShipment(przesylki, bufor.idBufor);
            List<String> guidy = new List<string>();
            System.Console.WriteLine("-----Nadawanie przesyłek");
            foreach (ENLabs.addShipmentResponseItemType przesylka in przesylkiWyslane)
            {
                if (przesylka.error == null)
                {
                    System.Console.WriteLine("[" + przesylka.numerNadania + "] : " + przesylka.guid);
                    guidy.Add(przesylka.guid);
                }
                else
                {
                    System.Console.WriteLine("Przesyłka o guidzie " + przesylka.guid + " nie została nadana z powodu błędów");
                    foreach (ENLabs.errorType error in przesylka.error)
                    {
                        System.Console.WriteLine("[" + error.errorNumber + "] " + error.errorDesc);
                    }
                }
            }

            return guidy;
        }

        private static ENLabs.errorType[] pobierzEtykietyZBuforu(EN client, ENLabs.buforType bufor, List<string> guidy)
        {
            System.Console.WriteLine("-----Pobranie etykiet");
            ENLabs.errorType[] errors;
            byte[] etykiety = client.getAddresLabelByGuidCompact(guidy.ToArray(), bufor.idBufor, out errors);
            SaveData(@"etykiety-bufor-" + bufor.idBufor.ToString() + ".pdf", etykiety);
            return errors;
        }

        private static ENLabs.urzadNadaniaFullType[] pobierzUrzedyNadaniaDlaKarty(EN c, ENLabs.kartaType karta)
        {
            ENLabs.urzadNadaniaFullType[] urzedyNadania = c.getUrzedyNadania();
            foreach (ENLabs.urzadNadaniaFullType urzadNadania in urzedyNadania)
            {
                System.Console.WriteLine("Urząd nadania dla karty " + karta.idKarta + " to " + urzadNadania.urzadNadania);
            }

            return urzedyNadania;
        }

        private static void ustawBiezacaKarte(EN c, ENLabs.kartaType karta)
        {
            c.setAktywnaKarta(karta.idKarta);
            System.Console.WriteLine("------Ustawiono bieżącą kartę na: " + karta.idKarta);
        }

        private static ENLabs.kartaType[] pobierzKartyZalogowanegoUzytkownika(EN c)
        {
            System.Console.Write("------Dostępne karty zalogowanego użytkownika:");
            var karty = c.getCurrentUserAccount().karta;
            foreach (ENLabs.kartaType karta in karty)
            {
                System.Console.Write(" " + karta.idKarta);
            }
            System.Console.WriteLine("");
            return karty;
        }

        private static ENLabs.profilType[] pobierzProfileZalogowanegoUzytkownika(EN c)
        {
            ENLabs.profilType[] profiles = c.getCurrentUserProfileList();
            System.Console.WriteLine("------Profile zalogowanego użytkownika");
            foreach (ENLabs.profilType profile in profiles)
            {
                System.Console.WriteLine(profile.idProfil + " " + profile.nazwaSkrocona);
            }

            return profiles;
        }

        private static ENLabs.buforType zdefiniujBufor(ENLabs.profilType profil, ENLabs.urzadNadaniaFullType urzadNadania)
        {
            ENLabs.buforType bufor = new ENLabs.buforType();
            bufor.dataNadania = DateTime.Now.AddDays(1);
            bufor.profil = profil;
            bufor.dataNadaniaSpecified = true;
            bufor.opis = "Nowy bufor webapi " + DateTime.Now.AddDays(1).ToString();
            bufor.urzadNadania = urzadNadania.urzadNadania;
            bufor.urzadNadaniaSpecified = true;
            return bufor;
        }

        public static ENLabs.przesylkaType createPocztexUiszczaOplateNadawca()
        {
            var pocztex = new ENLabs.uslugaKurierskaType();
            pocztex.adres = new ENLabs.adresType();
            pocztex.adres.nazwa = "Jan Kowalski";
            pocztex.adres.miejscowosc = "Gdynia";
            pocztex.adres.ulica = "10 Lutego";
            pocztex.adres.kodPocztowy = "81-301";

            pocztex.uiszczaOplate = ENLabs.uiszczaOplateType.NADAWCA;
            pocztex.uiszczaOplateSpecified = true;
            pocztex.termin = ENLabs.terminKurierskaType.EKSPRES24;
            pocztex.terminSpecified = true;
            var g = Guid.NewGuid();
            pocztex.guid = g.ToString();
            return pocztex;
        }

        public static ENLabs.przesylkaType createPocztexBledna()
        {
            var pocztex = new ENLabs.uslugaKurierskaType();
            pocztex.adres = new ENLabs.adresType();
            pocztex.adres.nazwa = "Jan Kowalski";
            pocztex.adres.miejscowosc = "Gdynia";
            pocztex.adres.ulica = "10 Lutego";
            pocztex.adres.kodPocztowy = "81-301";

            pocztex.uiszczaOplate = ENLabs.uiszczaOplateType.NADAWCA;
            pocztex.uiszczaOplateSpecified = true;
            pocztex.termin = ENLabs.terminKurierskaType.EKSPRES24;
            pocztex.terminSpecified = true;
            pocztex.guid = "123456";
            return pocztex;
        }
        public static bool SaveData(string fileNamePath, byte[] Data)
        {
            BinaryWriter Writer = null;
            try
            {
                Writer = new BinaryWriter(File.OpenWrite(fileNamePath));
                Writer.Write(Data);
                Writer.Flush();
            }
            catch
            {
                return false;
            }
            finally
            {
                Writer.Close();
            }

            return true;
        }
    }
}
