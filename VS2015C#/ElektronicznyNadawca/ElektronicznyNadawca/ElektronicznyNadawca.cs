using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElektronicznyNadawca
{
    class EN
    {
        String password;
        String user;
        Uri serviceUrl = new Uri("https://en-testwebapi.poczta-polska.pl/websrv/labs.php");
        Uri wsdlUrl = new Uri("https://en-testwebapi.poczta-polska.pl/websrv/labs.wsdl");
        ENLabs.ElektronicznyNadawca client = null;
        List<ENLabs.przesylkaType> przesylki = new List<ENLabs.przesylkaType>();
        public void setUserPassword(String user, String password)
        {
            this.password = password;
            this.user = user;
        }
        public void setServiceAddress(Uri url)
        {
            serviceUrl = url;
        }
        public void setWSDLAddress(Uri url)
        {
            wsdlUrl = url;
        }
        private void setCredential()
        {
            checkCredential();
            if (this.client == null)
            {
                this.client = new ENLabs.ElektronicznyNadawca();
                System.Net.NetworkCredential credential = new System.Net.NetworkCredential();
                credential.UserName = this.user;
                credential.Password = this.password;
                this.client.Url = serviceUrl.ToString();
                System.Net.CredentialCache cc = new System.Net.CredentialCache();
                cc.Add(new Uri(wsdlUrl.ToString()), "Basic", credential);
                this.client.Credentials = cc;
            }
        }

        private void checkCredential()
        {
            if (this.user == null || this.password == null)
            {
                throw new Exception("Nie ustawiono nazwy użytkownika i/lub hasła.");
            }
            if (this.serviceUrl == null || this.wsdlUrl == null)
            {
                throw new Exception("Nie ustawiono adresu webserwisu.");
            }
        }

        public String hello(String parameter)
        {
            setCredential();
            return this.client.hello(parameter);
        }
        public ENLabs.accountType[] getAccountList()
        {
            setCredential();
            ENLabs.accountType[] accountList = this.client.getAccountList();
            return accountList;
        }
        public ENLabs.profilType[] getUserProfileList(String userName)
        {
            setCredential();
            ENLabs.accountType[] accountList = this.getAccountList();
            foreach (ENLabs.accountType account in accountList)
            {
                if (account.userName.Equals(userName))
                {
                    return account.profil;
                }
            }
            throw new Exception("Nie znaleziono użytkownika " + userName);
        }
        public ENLabs.profilType[] getCurrentUserProfileList()
        {
            return this.getUserProfileList(this.user);
        }
        public ENLabs.accountType getCurrentUserAccount()
        {
            setCredential();
            foreach (ENLabs.accountType account in this.getAccountList())
            {
                if (account.userName.Equals(this.user))
                {
                    return account;
                }
            }
            throw new Exception("Błąd pobierania listy użytkowników");
        }
        public ENLabs.addShipmentResponseItemType[] addShipment(List<ENLabs.przesylkaType> przesylki, int? idBufor = null)
        {
            setCredential();
            if (idBufor.HasValue)
                return this.client.addShipment(przesylki.ToArray(), idBufor.Value, true);
            else
                return this.client.addShipment(przesylki.ToArray(), 0, false);
        }
        public ENLabs.errorType[] createEnvelopeBufor(ENLabs.buforType bufor)
        {
            setCredential();
            ENLabs.buforType[] b = { bufor };
            ENLabs.errorType[] errors;
            b = this.client.createEnvelopeBufor(b, out errors);
            bufor.idBufor = b.First().idBufor;
            foreach (ENLabs.errorType error in errors)
            {
                if (error.errorNumber != 0)
                {
                    throw new Exception("Nie udało się utowrznie Bufora z powodu błędów");
                }
            }
            return errors;
        }
        public ENLabs.errorType[] setAktywnaKarta(int idKarta)
        {
            setCredential();
            return this.client.setAktywnaKarta(idKarta);
        }
        public ENLabs.urzadNadaniaFullType[] getUrzedyNadania()
        {
            setCredential();
            return client.getUrzedyNadania();
        }
        public byte[] getAddresLabelByGuidCompact(string[] guid, int idBufor, out ENLabs.errorType[] errors)
        {
            setCredential();
            byte[] labels = this.client.getAddresLabelByGuidCompact(guid, idBufor, true, out errors);
            foreach (ENLabs.errorType error in errors)
            {
                if (error.errorNumber != 0)
                {
                    throw new Exception("Nie udało się pobrać etykiet z powodu błędu");
                }
            }
            return labels;
        }
    }
}
