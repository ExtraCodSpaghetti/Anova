using Braintree;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Anova_DataAccess.Repository.IRepository;
using Anova_Models;
using Anova_Models.ViewModels;
using Anova_Utility;
using Anova_Utility.Brain_Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Anova.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly IApplicationUserRepository _userRepo;
        private readonly IProductRepository _prodRepo;
        private readonly IInquiryHeaderRepository _inqHRepo;
        private readonly IInquiryDetailRepository _inqDRepo;
        private readonly IOrderHeaderRepository _orderHRepo;
        private readonly IOrderDetailRepository _orderDRepo;
        private readonly IBrainTreeGate _brain;

        [BindProperty]
        public ProductUserVM ProductUserVM { get; set; }

        public CartController(IWebHostEnvironment webHostEnvironment, IEmailSender emailSender,
            IApplicationUserRepository userRepo, IProductRepository prodRepo,
            IInquiryHeaderRepository inqHRepo, IInquiryDetailRepository inqDRepo,
            IOrderHeaderRepository orderHRepo, IOrderDetailRepository orderDRepo, IBrainTreeGate brain)
        {
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
            _brain = brain;
            _userRepo = userRepo;
            _prodRepo = prodRepo;
            _inqDRepo = inqDRepo;
            _inqHRepo = inqHRepo;
            _orderDRepo = orderDRepo;
            _orderHRepo = orderHRepo;
        }

        public IActionResult Index()
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Count() > 0)
            {
                //session exsits
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();
            IEnumerable<Product> prodListTemp = _prodRepo.GetAll(u => prodInCart.Contains(u.Id));
            IList<Product> prodList = new List<Product>();

            foreach (var cartObj in shoppingCartList)
            {
                Product prodTemp = prodListTemp.FirstOrDefault(u => u.Id == cartObj.ProductId);
                prodTemp.TempAmounts = cartObj.Amounts;
                prodList.Add(prodTemp);
            }

            return View(prodList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Index")]
        public IActionResult IndexPost(IEnumerable<Product> ProdList)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            foreach (Product prod in ProdList)
            {
                shoppingCartList.Add(new ShoppingCart { ProductId = prod.Id, Amounts = prod.TempAmounts });
            }

            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);
            return RedirectToAction(nameof(Summary));
        }

        public IActionResult Summary()
        {
            ApplicationUser applicationUser;

            if (User.IsInRole(WC.AdminRole))
            {
                if (HttpContext.Session.Get<int>(WC.SessionInquiryId) != 0)
                {
                    //cart has been loaded using an inquiry
                    InquiryHeader inquiryHeader = _inqHRepo.FirstOrDefault(u => u.Id == HttpContext.Session.Get<int>(WC.SessionInquiryId));
                    applicationUser = new ApplicationUser()
                    {
                        Email = inquiryHeader.Email,
                        FullName = inquiryHeader.FullName,
                        PhoneNumber = inquiryHeader.PhoneNumber
                    };
                }
                else
                {
                    applicationUser = new ApplicationUser();
                }
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                //var userId = User.FindFirstValue(ClaimTypes.Name);

                applicationUser = _userRepo.FirstOrDefault(u => u.Id == claim.Value);
            }

            var gateway = _brain.GetGateway();
            var clientToken = gateway.ClientToken.Generate();
            ViewBag.ClientToken = clientToken;

            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Count() > 0)
            {
                //session exsits
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();
            IEnumerable<Product> prodList = _prodRepo.GetAll(u => prodInCart.Contains(u.Id));

            ProductUserVM = new ProductUserVM()
            {
                ApplicationUser = applicationUser,
            };

            foreach (var cartObj in shoppingCartList)
            {
                Product prodTemp = _prodRepo.FirstOrDefault(u => u.Id == cartObj.ProductId);
                prodTemp.TempAmounts = cartObj.Amounts;
                ProductUserVM.ProductList.Add(prodTemp);
            }

            return View(ProductUserVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(IFormCollection collection, ProductUserVM ProductUserVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //we need to create an order
            //var orderTotal = 0.0;
            //foreach(Product prod in ProductUserVM.ProductList)
            //{
            //    orderTotal += prod.Price * prod.TempSqFt;
            //}

            //we need to create an inquiry

            OrderHeader orderHeader = new OrderHeader()
            {
                CreatedByUserId = claim.Value,
                FinalOrderTotal = ProductUserVM.ProductList.Sum(x => x.TempAmounts * x.Price),
                City = ProductUserVM.ApplicationUser.City,
                StreetAddress = ProductUserVM.ApplicationUser.StreetAddress,
                State = ProductUserVM.ApplicationUser.State,
                PostalCode = ProductUserVM.ApplicationUser.PostalCode,
                FullName = ProductUserVM.ApplicationUser.FullName,
                Email = ProductUserVM.ApplicationUser.Email,
                PhoneNumber = ProductUserVM.ApplicationUser.PhoneNumber,
                OrderDate = DateTime.Now,
                OrderStatus = WC.StatusPending
            };
            _orderHRepo.Add(orderHeader);
            _orderHRepo.Save();

            var PathToTemplate = _webHostEnvironment.WebRootPath + Path.DirectorySeparatorChar.ToString()
           + "templates" + Path.DirectorySeparatorChar.ToString() +
           "Inquiry.html";

            var subject = "New Inquiry";
            string HtmlBody = "";
            using (StreamReader sr = System.IO.File.OpenText(PathToTemplate))
            {
                HtmlBody = sr.ReadToEnd();
            }
            //Name: { 0}
            //Email: { 1}
            //Phone: { 2}
            //Products: {3}

            StringBuilder productListSB = new StringBuilder();
            decimal totalOrderPrice = 0; // Переменная для хранения общей стоимости заказа

            foreach (var prod in ProductUserVM.ProductList)
            {
                decimal productTotal = (decimal)(prod.Price * prod.TempAmounts); // Рассчитываем стоимость текущего товара
                totalOrderPrice += productTotal; // Добавляем стоимость текущего товара к общей сумме
                productListSB.Append($" - Name: {prod.Name} <span style='font-size:14px;'> (Amounts: {prod.TempAmounts} with Total Price: {productTotal})</span><br />");
            }

            // Добавляем информацию о общей стоимости заказа
            productListSB.Append($"<br /><strong>Total Order Price: {totalOrderPrice}</strong> <br /><strong>Your order identification number : {orderHeader.Id}</strong> ");

            string messageBody = string.Format(HtmlBody,
                ProductUserVM.ApplicationUser.FullName,
                ProductUserVM.ApplicationUser.Email,
                ProductUserVM.ApplicationUser.PhoneNumber,
                productListSB.ToString());

            await _emailSender.SendEmailAsync(WC.EmailAdmin, subject, messageBody);

            foreach (var prod in ProductUserVM.ProductList)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    OrderHeaderId = orderHeader.Id,
                    PricePerAmounts = prod.Price,
                    Amounts = prod.TempAmounts,
                    ProductId = prod.Id
                };
                _orderDRepo.Add(orderDetail);
            }
            _orderDRepo.Save();
            TempData[WC.Success] = "Inquiry submitted successfully";

            string nonceFromTheClient = collection["payment_method_nonce"];

            var request = new TransactionRequest
            {
                Amount = Convert.ToDecimal(orderHeader.FinalOrderTotal),
                PaymentMethodNonce = nonceFromTheClient,
                OrderId = orderHeader.Id.ToString(),
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                }
            };

            var gateway = _brain.GetGateway();
            Result<Transaction> result = gateway.Transaction.Sale(request);

            if (result.Target.ProcessorResponseText == "Approved")
            {
                orderHeader.TransactionId = result.Target.Id;
                orderHeader.OrderStatus = WC.StatusApproved;

                //  SweetAlert2
                TempData["SwalIcon"] = "info"; // success, error, info, warning
                TempData["SwalTitle"] = "Payment has been made successfully!";
                TempData["SwalText"] = "<hr /> You will receive a message with information about your order.<br /> In case of questions about delivery or other questions, please call the phone numbers in the Contact Us tab. There you can also use a convenient form for contacting us by e-mail.";
            }
            else
            {
                orderHeader.OrderStatus = WC.StatusCancelled;

                TempData["SwalIcon"] = "error";
                TempData["SwalTitle"] = "Error";
                TempData["SwalText"] = "The payment was declined. Please try again.";
            }
            _orderHRepo.Save();
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        //public IActionResult InquiryConfirmation(int id = 0)
        //{
        //    OrderHeader orderHeader = _orderHRepo.FirstOrDefault(u => u.Id == id);
        //    HttpContext.Session.Clear();
        //    return View(orderHeader);
        //}

        public IActionResult Remove(int id)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Count() > 0)
            {
                //session exsits
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            shoppingCartList.Remove(shoppingCartList.FirstOrDefault(u => u.ProductId == id));
            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCart(IEnumerable<Product> ProdList)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            foreach (Product prod in ProdList)
            {
                shoppingCartList.Add(new ShoppingCart { ProductId = prod.Id, Amounts = prod.TempAmounts });
            }

            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}