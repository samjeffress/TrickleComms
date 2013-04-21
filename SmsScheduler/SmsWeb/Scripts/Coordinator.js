
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
        if ($('#TimeSeparatorSeconds').val().length > 0) {
            // calculate the end date
            

        } else if ($('#SendAllBy').val().length > 0) {
            // Check if the dates send messages overnight
            var startDateTime = $.datepicker.parseDate("dd/mm/yy", $('#StartTime').val());
            var startDate = new Date(startDateTime.getFullYear(), startDateTime.getMonth(), startDateTime.getDate());
            var endDateTime = $.datepicker.parseDate("dd/mm/yy", $('#SendAllBy').val());
            var endDate = new Date(endDateTime.getFullYear(), endDateTime.getMonth(), endDateTime.getDate());
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