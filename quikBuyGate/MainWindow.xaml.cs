using Io.Gate.GateApi.Api;
using Io.Gate.GateApi.Client;
using Io.Gate.GateApi.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Net.WebSockets;
using Websocket.Client;
using System.Threading;
using quikBuyGate.Model;

namespace quikBuyGate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        SpotApi spotApi;
        Configuration config;
        Order createdBuy;
        Order orderResult;
        WebsocketClient client;
        long timeStart = 0;
        long currentTimeServer = 0;

        private void DisableControlElement()
        {
            tbNumbV1.IsEnabled = true;
            tbPrice1.IsEnabled = true;
            tbNumbV2.IsEnabled = true;
            tbPrice2.IsEnabled = true;
            btnBuyOrder1.IsEnabled = true;
            btnBuyOrder2.IsEnabled = true;
            CancelAllOrders.IsEnabled = true;
            SalleGrid.IsEnabled = true;
        }

        private void EnabledControlElement()
        {
            tbNumbV1.IsEnabled = false;
            tbPrice1.IsEnabled = false;
            tbNumbV2.IsEnabled = false;
            tbPrice2.IsEnabled = false;
            btnBuyOrder1.IsEnabled = false;
            btnBuyOrder2.IsEnabled = false;
            CancelAllOrders.IsEnabled = false;
            SalleGrid.IsEnabled = false;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnabledControlElement();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings s = new Settings();
            s.Show();
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            config = new Configuration
            {
                // Setting basePath is optional. It defaults to https://api.gateio.ws/api/v4
                // ApiV4Key = "06663e702992c511a1b12cef99519eaf",
                //ApiV4Secret = "bb772c0a98614e4753a156cb9d0503fb0cb1e13aac4f42e35d81d45ddfe69b6a",
                ApiV4Key = Properties.Settings.Default.API,
                ApiV4Secret = Properties.Settings.Default.Secret
            };

            spotApi = new SpotApi(config);

            var res = spotApi.ListTickers().Where(p => p.CurrencyPair.Substring(p.CurrencyPair.Length - 4) == "USDT");
            res = res.Where(r => r.Last == "0").ToList();

            var ress = spotApi.ListSpotAccounts();

            foreach (var cur in res)
            {
                cbToken.Items.Add(cur.CurrencyPair);
            }
            cbToken.SelectedIndex = 0;

            try
            {
                List<SpotAccount> accounts = spotApi.ListSpotAccounts("USDT");
                DisableControlElement();
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка авторизации. Проверьте ключи в настройках");
                return;
            }


        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void NumericOnly(System.Object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextNumeric(e.Text);
        }
        private static bool IsTextNumeric(string str)
        {
            if (str == ".")
            {
                return false;
            }
            else
            {
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[^0-9]");
                return reg.IsMatch(str);
            }

        }

        private async void btnBuyOrder1_Click(object sender, RoutedEventArgs e)
        {
            await PlaceOrderBuy(tbPrice1.Text, tbNumbV1.Text);
        }

        private async Task PlaceOrderBuy(string lastPrice, string QuantityUSDT)
        {
            string currencyPair = cbToken.Text;
            string currency = currencyPair.Split('_')[1];

            CurrencyPair pair = spotApi.GetCurrencyPair(currencyPair);

            string minAmount = pair.MinQuoteAmount;
            decimal available;
            decimal orderAmount;
            List<SpotAccount> accounts;

            try
            {
                orderAmount = decimal.Parse(QuantityUSDT, CultureInfo.InvariantCulture) / decimal.Parse(lastPrice, CultureInfo.InvariantCulture);
                accounts = spotApi.ListSpotAccounts(currency);

                available = Convert.ToDecimal(accounts[0].Available, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при парсинге " + ex.Message);
                return;
            }

            Order order = new Order(currencyPair: currencyPair, amount: orderAmount.ToString(CultureInfo.InvariantCulture), price: lastPrice)
            {
                Account = Order.AccountEnum.Spot,
                Side = Order.SideEnum.Buy
            };

            for (int i = 0; i < 20; i++)
            {
                try
                {
                    createdBuy = await spotApi.CreateOrderAsync(order);
                    ListBuy.Items.Add($"{i} попытка УСПЕХ");
                    WBStart();
                    break;
                }
                catch (Exception ex)
                {
                    ListBuy.Items.Add($"{i} попытка размещения ПРОВАЛЕНА");
                }
            }
        }
        private async void btnBuyOrder2_Click(object sender, RoutedEventArgs e)
        {
            await PlaceOrderBuy(tbPrice2.Text, tbNumbV2.Text);
        }

        private void btnSaleOrder1_Click(object sender, RoutedEventArgs e)
        {
            OrderSellPair(tbOrderSaleP8.Text);
        }

        private void OrderSellPair(string price)
        {
            try
            {
                var res = spotApi.ListSpotAccounts(cbToken.Text.Split('_')[0]);
                //var res = spotApi.ListSpotAccounts("BTT");
                //MatchCollection amount = Regex.Matches(res.First().Available, @"\d+.\d");
                decimal amountRound = decimal.Parse(res.First().Available, CultureInfo.InvariantCulture);
                //amountRound = Math.Round(amountRound, 1);
                Order order = new Order(currencyPair: cbToken.Text, amount: (amountRound*(decimal)0.99).ToString().Replace(",", "."), price: price.Replace(",","."))
                {
                    Account = Order.AccountEnum.Spot,
                    Side = Order.SideEnum.Sell
                };

                Order created = spotApi.CreateOrder(order);

                ListSell.Items.Add($"Ордер на продажу {cbToken.Text} по стоимости {price} размещен");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при размещении ордера на продажу" + ex);
            }

        }

        private void btnSaleOrder2_Click(object sender, RoutedEventArgs e)
        {
            OrderSellPair(tbOrderSaleP5.Text);

        }

        private  void btnSaleOrder3_Click(object sender, RoutedEventArgs e)
        {
            OrderSellPair(tbOrderSaleP3.Text);
        }

        private async void CancelAllOrders_Click(object sender, RoutedEventArgs e)
        {
            

            //WBStart();
            try
            {
                List<OpenOrders> openOrders = await spotApi.ListAllOpenOrdersAsync();
                foreach (var oo in openOrders)
                {
                    var res = spotApi.CancelOrders(oo.CurrencyPair);
                    ListSell.Items.Add($"Ордер {res.First().CurrencyPair} отменен");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ордера не отменены" + ex.Message);
            }

        }
        private async void WBStart()
        {
            Uri url = new Uri("wss://api.gateio.ws/ws/v4/");
            ManualResetEvent exitEvent = new ManualResetEvent(false);

            client = new WebsocketClient(url);

            client.MessageReceived.Subscribe(msg => UpdatePriceSell(msg.Text));
            await client.Start();
            client.Send("{\"channel\": \"spot.tickers\",\"event\": \"subscribe\",\"payload\": [\""+ cbToken.Text+ "\"]}");
        }
        
        private void UpdatePriceSell(string response)
        {
            
            Dispatcher.Invoke(() =>
            {
                List<SpotAccount> accounts = spotApi.ListSpotAccounts(cbToken.Text.Replace("_USDT", ""));
                var res=Newtonsoft.Json.JsonConvert.DeserializeObject<DataTicker>(response);
                if(res.result.last!=null)
                {
                    decimal lastPrice = decimal.Parse(res.result.last,CultureInfo.InvariantCulture);
                    lbProfit.Content = Math.Round((decimal.Parse(accounts[0].Available, CultureInfo.InvariantCulture) * lastPrice),3)+" USDT";
                    tbLastPrice.Text = lastPrice.ToString();
                    tbOrderSaleM3.Text = (lastPrice * (decimal)0.97).ToString();
                    tbOrderSaleM5.Text = (lastPrice * (decimal)0.95).ToString();
                    tbOrderSaleM8.Text = (lastPrice * (decimal)0.92).ToString();

                    tbOrderSaleP3.Text = (lastPrice * (decimal)1.03).ToString();
                    tbOrderSaleP5.Text = (lastPrice * (decimal)1.05).ToString();
                    tbOrderSaleP8.Text = (lastPrice * (decimal)1.08).ToString();
                }
                
                
            }); 
        }
        private async void btnSync_Click(object sender, RoutedEventArgs e)
        {

            WBStart();
        }

        private void btnSaleOrderM3_Click(object sender, RoutedEventArgs e)
        {
            OrderSellPair(tbOrderSaleM3.Text);
        }

        private void btnSaleOrderM5_Click(object sender, RoutedEventArgs e)
        {
            OrderSellPair(tbOrderSaleM5.Text);
        }

        private void btnSaleOrderM8_Click(object sender, RoutedEventArgs e)
        {
            OrderSellPair(tbOrderSaleM8.Text);
        }

        private void btnSaleOrderMarket_Click(object sender, RoutedEventArgs e)
        {
            OrderSellPair(tbLastPrice.Text);
        }
    }
}
