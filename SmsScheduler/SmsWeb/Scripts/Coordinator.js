
function ShowSendAll() {
    $('#TimeSeparatedDiv').hide(200);
    $('#SendAllByDiv').show(200);
    $('#TimeSeparatorSeconds').val('');
    $('#SendAllAtOnce').val(false);
    $("#OvernightWarning").hide();
    SetHighlightedButton("#SendAllByButton");
}

function ShowTimeSeparated() {
    $('#SendAllByDiv').hide(200);
    $('#TimeSeparatedDiv').show(200);
    $('#SendAllBy').val('');
    $('#SendAllAtOnce').val(false);
    $("#OvernightWarning").hide();
    SetHighlightedButton("#TimeSeparatedButton");
}

function HideTimingOptions() {
    $('#SendAllByDiv').hide(200);
    $('#TimeSeparatedDiv').hide(200);
    $('#SendAllBy').val('');
    $('#SendAllAtOnce').val(true);
    $("#OvernightWarning").hide();
    SetHighlightedButton("#AllAtOnceButton");
}

function CheckSendAll() {
    if ($('#SendAllBy').val().length == 0) {
        $('#SendAllByDiv').hide();
    } else {
        ShowSendAll();
    }
}

function CheckTimeSeparated() {
    if ($('#TimeSeparatorSeconds').val().length == 0) {
        $('#TimeSeparatedDiv').hide();
    } else {
        ShowTimeSeparated();
    }
}

function SetHighlightedButton(idToHighlight) {
    $('#SendAllByButton').removeClass("SelectedButton");
    $('#SendAllByButton').addClass("UnSelectedButton");
    $('#TimeSeparatedButton').removeClass("SelectedButton");
    $('#TimeSeparatedButton').addClass("UnSelectedButton");
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
        if ($('#TimeSeparatorSeconds').length > 0 && $('#TimeSeparatorSeconds').val().length > 0) {
            // calculate the end date
            var timeBetweenMessages = $('#TimeSeparatorSeconds').val();
            var numberOfMessages = MobileNumberCount();
            var secondsToSendAllMessages = numberOfMessages * timeBetweenMessages;
            var endDateTime = new Date();
            endDateTime.setTime(endDateTime.getTime() + secondsToSendAllMessages * 1000);
            endDate = new Date(endDateTime.getFullYear(), endDateTime.getMonth(), endDateTime.getDate());
            if (startDate < endDate) {
                $("#OvernightWarning").removeClass("blockHidden");
                $("#OvernightWarning").show(200);
            } else {
                $("#OvernightWarning").addClass("blockHidden");
                $("#OvernightWarning").hide(200);
            }
        } else if ($('#SendAllBy').val().length > 0) {
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