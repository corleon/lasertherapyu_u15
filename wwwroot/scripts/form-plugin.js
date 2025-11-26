// REFACTOR and do correctly !!!!!!!!!!!!!!!!!!!!!!!!!

(function ($) {
    $.fn.formProcessor = function (options) {
        var o = $.extend({
            element: 'li',
            handlerUrl: '/handlers/forms/form.ashx',
            pageId: '',
            errorMessage: 'There was an error processing your request',
            errorClass: 'error',
            requiredClass: 'required',
            submitButtonId: 'submit',
            confirmationContainerId: 'contact-confirm',
            placeholderButtonClass: 'submit-placeholder',
            passwordFieldID: '',
            passwordConfirmFieldID: '',
            elementToRemove: [],
            redirectTo: '',
            actions: ['email', 'save']
        }, options || {});

        var order = 1;
        var errorCount = 0;
        var fieldArray = [];
        var checkboxArray = [];
        var radioButtonArray = [];
        var additionalInformationArray = [];
        var xhrFormProcessor;

        this.find('>' + o.element).each(function () {
            $this = $(this);
            var requiredField = $this.find('.' + o.requiredClass);

            //console.log(requiredField.attr('class'));
            if (requiredField.attr('type') == 'radio') {
                validateRadio($this, requiredField);
            } else {
                validateElement($this, requiredField);
            }

        });

        validatePassword(o.passwordFieldID, o.passwordConfirmFieldID);

        if (this.find('.' + o.errorClass).length > 0) {
            //    $('html, body').animate({
            //        scrollTop: $("#content").offset().top
            //    }, 0);

            return this.find('.' + o.errorClass).length;
        }

        this.find('.' + o.placeholderButtonClass).show();
        this.find('#' + o.submitButtonId).hide();

        this.find('>' + o.element).each(function () {
            var $this = $(this);
            
            if ($this.hasClass('no') == false) {
                if ($this.children().is('input[type=text]') || $this.children().is('input[type=password]')) {
                    var input = $this.find('input');
                    var fieldName = getLabelName(input, $this);

                    fieldArray.push('{ "id" : "' + input.attr('id') + '", "name": "' + fieldName + '", "value":"' + stringCleaner(input.val()) + '", "order": ' + order + '}');
                } else if ($this.children().is('input[type=email]')) {
                    var input = $this.find('input[type=email]');
                    var fieldName = getLabelName(input, $this);

                    fieldArray.push('{ "id" : "' + input.attr('id') + '","name": "' + fieldName + '", "value":"' + stringCleaner(input.val()) + '", "order": ' + order + '}');
                } else if ($this.children().is('input[type=tel]')) {
                    var input = $this.find('input[type=tel]');
                    var fieldName = getLabelName(input, $this);

                    fieldArray.push('{ "id" : "' + input.attr('id') + '","name": "' + fieldName + '", "value":"' + stringCleaner(input.val()) + '", "order": ' + order + '}');
                }

                if ($this.find('select')) {
                    var select = $this.find('select');
                    var fieldName = getLabelName(select, $this);

                    fieldArray.push('{ "id" : "' + select.attr('id') + '","name": "' + fieldName + '", "value":"' + $('option:selected', select).val() + '", "order": ' + order + '}');
                }

                if ($this.find('input').is('input[type=checkbox]')) {
                    var checkbox = $this.find('input[type=checkbox]');
                    var dataType = checkbox.data('type') != null ? checkbox.data('type') : checkbox.closest('span').data('type');
                    var typeName = '';

                    if (dataType != null) {
                        typeName = dataType;
                    }

                    checkboxArray.push(formatCheckboxJSON(checkbox, order, typeName, $this));
                }

                //if ($this.find('input').is('input[type=radio]')) {
                //    var radio = $this.find('input[type=radio]');

                //    radioButtonArray.push(formatRadioButtonJSON(radio, order, '', $this));
                //}

                if ($this.children().is('textarea')) {
                    var textarea = $this.find('textarea');
                    var fieldName = getLabelName(textarea, $this);

                    fieldArray.push('{ "id" : "' + textarea.attr('id') + '","name": "' + fieldName + '", "value":"' + stringCleaner(textarea.val()) + '", "order": ' + order + '}');
                }

                if ($this.children().is('input[type=hidden]')) {
                    var hidden = $this.find('input[type=hidden]');
                    var fieldName = getLabelName(hidden, $this);

                    fieldArray.push('{"id" : "' + hidden.attr('id') + '", "name": "' + fieldName + '", "value":"' + stringCleaner(hidden.val()) + '", "order": ' + order + '}');
                }

                order++;
            }
        });

        var json = '{"fieldlist": [' + fieldArray + '], "checkboxes": [' + checkboxArray + '], "radioButtons": [' + radioButtonArray + '], "pageId" : "' + o.pageId + '"}';

        $(document).loading({
            button: $('#' + o.submitButtonId),
            showLoading:true
         });

        xhrFormProcessor = $.post(o.url, { json: json }, function (data) {
            
            if (data == 'True') {
                for (var d = 0; d <= o.elementToRemove.length; d++) {
                    if (o.elementToRemove[d] != null) {
                        o.elementToRemove[d].remove();
                    }
                }

                if (o.redirectTo != '') {
                   window.location = o.redirectTo;                        
                }
                    
                $('#' + o.confirmationContainerId).fadeIn('fast');
            } else {
                alert(o.errorMessage);

                $('.' + o.placeholderButtonClass).hide();
                $('#' + o.submitButtonId).show();
            }

            $(document).loading({
                button: $('#' + o.submitButtonId),
                showLoading: false,
                fullButtonClass: ' btn btn-blue'
            });
        });


        function validateElement(container, field) {
            if (field.val() != null) {// && field.attr('type') != 'password') {                    
                if (field.is(':visible') && (field.attr('type') === 'email' && !isValidEmailAddress(field.val()) || field.val().length == 0)) {
                    container.addClass(o.errorClass);
                } else {
                    container.removeClass(o.errorClass);
                }
            }
            if (container.find('input').is(':checked')) {
                container.removeClass(o.errorClass);
            }
        }

        function validateRadio(container, field) {
            if (field.val() != null && field.is(':visible')) {
                if (field.is(':checked') === false) {
                    container.addClass(o.errorClass);
                } else {
                    container.removeClass(o.errorClass);
                }
            }
        }

        function validatePassword(passwordFieldID, passwordConfimationID) {
            if (passwordFieldID.length != 0 && passwordConfimationID.length != 0) {
                var $password = $('#' + passwordFieldID);
                var $confirm = $('#' + passwordConfimationID);

                if (($password.is(':visible') && $confirm.is(':visible')) && ($password.val() != $confirm.val())) {
                    $confirm.closest('li').addClass(o.errorClass);
                }
            }
        }

        function formatCheckboxJSON(ele, order, type, container) {
            var name = ele.closest('div').children('label:first').text();
            var json = '';
            var tempArray = [];

            ele.each(function () {

                if ($(this).is(':checked')) {
                    json = '{"name":"' + getLabelName($(this), container) + '", "value":"true"}';
                } else {
                    json = '{"name":"' + getLabelName($(this), container) + '", "value":"false"}';
                }
            });

            return json;
        }

        function formatRadioButtonJSON(ele, order, type, container) {
            var name = ele.closest(container).children('label:first').text();
            var json = '{ "name": "' + name + '", "order": ' + order + ', "RadioButtonValues": [';

            ele.each(function () {
                if ($(this).is(':checked')) {
                    var lbl = getLabelName($(this), container);
                    json += '{"name":"' + lbl + '", "value":"' + lbl + '"}';
                }
            });

            json += ']}';

            return json;
        }

        function getLabelName(ele, container) {
            var labelName = ele.next('label').text();
            var fieldName = '';

            if (labelName == '') {
                labelName = ele.prev('label').text();
            }

            if (labelName == '') {
                fieldName = ele.closest(container).children('label:first').text();
            }

            if (labelName == '' && fieldName == '' && ele.attr('placeholder') != null) {
                fieldName = ele.attr('placeholder');
            }

            if (labelName == '' && fieldName == '') {
                fieldName = ele.attr('name');
            }

            var retVal = labelName != '' ? labelName : fieldName;

            return $.trim(retVal);
        }


        function stringCleaner(str) {
            str = stripHtml(str);

            return $.trim(str);
        }

        function stripHtml(str) {
            return $.trim(jQuery('<span />', { html: str }).text());
        }

        function isValidEmailAddress(emailAddress) {
            var pattern = new RegExp(/^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$/i);
            return pattern.test(emailAddress);
        }
    }
})(jQuery);

