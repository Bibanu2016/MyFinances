//
// The script uses the partial view ../Pages/Shared/_ToastMessage.cshtml elements to display notifications on a page
//
function displayToast(a, b) {
    const s = new bootstrap.Toast(document.getElementById("successToast"));
    const w = new bootstrap.Toast(document.getElementById("warningToast"));
    const e = new bootstrap.Toast(document.getElementById("errorToast"));
    var sm = document.getElementById("successMessage");
    var wm = document.getElementById("warningMessage");
    var em = document.getElementById("errorMessage");
    if (a != null && b != null) {
        switch (b) {
            case "0":
                sm.innerHTML = a;
                s.show();
                break;
            case "1":
                wm.innerHTML = a;
                w.show();
                break;
            case "2":
                em.innerHTML = a;
                e.show();
                break;
            default:
                wm.innerHTML = "Alert type not defined! \n " + a;
                break;
        }
    }
    else {
        alert("please try again later, there was an error in processing the message");
    }

};
