// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//header
window.onscroll = function () {
    var header = document.querySelector('header');
    if (window.pageYOffset > 150) {
        header.classList.add('sticky');
    } else {
        header.classList.remove('sticky');
    }
}

//navbar navigation
   
    $(document).ready(function () {
        $(".navbar-toggler").click(function () {
            if ($("#navbarSupportedContent").hasClass("show")) {
                $("#navbarSupportedContent").removeClass("show");
            } else {
                $("#navbarSupportedContent").addClass("show");
            }
        });
    });




// footer
function stickyFooter() {
    var footer = document.querySelector("footer");
    var footerHeight = footer.offsetHeight;
    var windowHeight = window.innerHeight;
    var bodyHeight = document.body.offsetHeight;

    if (bodyHeight < windowHeight) {
        footer.style.position = "fixed";
        footer.style.bottom = "0";
        footer.style.width = "100%";
    } else {
        footer.style.position = "static";
    }
}

window.addEventListener('load', function () {
    stickyFooter();
});

window.addEventListener('resize', function () {
    stickyFooter();
});
