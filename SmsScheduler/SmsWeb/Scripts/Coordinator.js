
function ShowSendAll() {
    $('#SendAllAtOnce').val(false);
    SetHighlightedButton("#SendAllByButton");
}


function SetHighlightedButton(idToHighlight) {
    $('#SendAllByButton').removeClass("SelectedButton");
    $('#SendAllByButton').addClass("UnSelectedButton");
    $('#AllAtOnceButton').removeClass("SelectedButton");
    $('#AllAtOnceButton').addClass("UnSelectedButton");
    
    $(idToHighlight).removeClass("UnSelectedButton");
    $(idToHighlight).addClass("SelectedButton");
}

function CheckIfMessagesWillBeSentOvernight() {
    if ($('#StartTime').val().length > 0) {
        var endDate;
        var startDateTime = $.datepicker.parseDate("dd/mm/yy", $('#StartTime').val());
        var startDate = new Date(startDateTime.getFullYear(), startDateTime.getMonth(), startDateTime.getDate());
        if ($('#SendAllBy').val().length > 0) {
            // Check if the dates send messages overnight
            var endDateTime = $.datepicker.parseDate("dd/mm/yy", $('#SendAllBy').val());
            endDate = new Date(endDateTime.getFullYear(), endDateTime.getMonth(), endDateTime.getDate());
            if (startDate < endDate) {
                $("#OvernightWarning").removeClass("blockHidden");
                $("#OvernightWarning").show(200);
            } else {
                $("#OvernightWarning").addClass("blockHidden");
                $("#OvernightWarning").hide(200);
            }
        }
    }
}

function MobileNumberCount() {
    var numbers = $('#Numbers').val().split(',');
    var numberCount = 0;
    for (var i = 0; i < numbers.length; i++) {
        if (numbers[i].replace(" ", "").length > 2)
            numberCount++;
    }
    return numberCount;
}

function showOrHideTiming() {
    if (MobileNumberCount() > 1) {
        $('#CoordinationTiming').show(600);
    } else {
        $('#CoordinationTiming').hide(600);
    }
}