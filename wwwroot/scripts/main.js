//-----------------------------------------
// Laser Therapy Univeristy
// 
// Last Updated By: Ryan Perry
// Last Updated: 06/18/2013
//-----------------------------------------


//var $siteheader = $('.site-header');
//var $sitefooter = $('.site-footer');
var $sitenav = $('.content-navigation');
var $maincontainer = $('.main-container');

var _small = true;
var _narrow = false;
var _large = false;
var _viewing = 's';

var _sLoaded = false;
var _nLoaded = false;
var _lLoaded = false;

var _subnavopen = false;
var _subnavsame = false;
var _navtemp;
var _navclone;


jQuery(function ($) {
    
    //$('<div id="debug" />').appendTo('body');
    var $debug = $('#debug');
    
	if ($('#mainform[action="/faqs"]').length) {
		$('body').addClass('faqPage');
	}else if ($('.breadcrumb-nav span').text() == 'Future Webinars') {
		$('.listing-item .listing-item-btn .btn').attr('target', '_blank');
	}

	function loggedInCheck(){
		if ($('.header-account-loginlogout').children().length > 1) {
			$('body').addClass('loggedIn');
		}
	}
	
	$('.dashboard-item ul li:first-child a').prepend('<br />');
	
    //---------------------------------------------------
    // MediaQueires Break Point Test
    //---------------------------------------------------
    //if (Modernizr.mq('only screen and (max-width:650px)')) {
    //    _small = true;
    //    _narrow = false;
    //    _large = false;

    //    if (_sLoaded === false) {
    //        $.getJSON('/handlers/laseru/navigation.ashx', { pID: $('#hdnPageID').val() }, function (headerz) {
    //        }).success(function (headerz) {
    //            SmallView(headerz);
				//loggedInCheck();

    //        $(document).loadAjaxSwapImages({
    //            images: $('img.ress'),
    //            size: 'small'
    //        });
    //        });

    //        _sLoaded = true;
    //    }

    //} else if (Modernizr.mq('only screen and (min-width:651px) and (max-width:851px)')) {
    //    _narrow = true;
    //    _small = false;
    //    _large = false;

    //    if (_nLoaded === false) {
    //        $.getJSON('/handlers/laseru/navigation.ashx', { pID: $('#hdnPageID').val() }, function (headerz) {
    //        }).success(function (headerz) {
    //            NarrowView(headerz);
				//loggedInCheck();

    //            $(document).loadAjaxSwapImages({
    //                images: $('img.ress'),
    //                size: 'narrow'
    //            });
    //        });

    //        _nLoaded = true;
    //    }

    //} else if (Modernizr.mq('only screen and (min-width:851px)')) {
    //    _large = true;
    //    _small = false;
    //    _narrow = false;

    //    if (_lLoaded === false) {
    //        $.getJSON('/handlers/laseru/navigation.ashx', {pID: $('#hdnPageID').val()}, function (headerz) {
    //        }).success(function (headerz) {
    //            LargeView(headerz);
				//$('.site-main-nav-link').has('ul:hidden').addClass('hasSubNav');
				//loggedInCheck();

    //            $(document).loadAjaxSwapImages({
    //                images: $('img.ress'),
    //                size: 'large'
    //            });
    //        });

    //        _lLoaded = true;
    //    }
    //} else {
    //    _large = true;
    //    _small = false;
    //    _narrow = false;

    //    if (_lLoaded === false) {
    //        $.getJSON('/handlers/laseru/navigation.ashx', { pID: $('#hdnPageID').val() }, function (headerz) {
    //        }).success(function (headerz) {
    //            LargeView(headerz);
				//$('.site-main-nav-link').has('ul:hidden').addClass('hasSubNav');
				//loggedInCheck();

    //        });

    //        $(document).loadAjaxSwapImages({
    //            images: $('img.ress'),
    //            size: 'large'
    //        });

    //        _lLoaded = true;
    //    }
    //}


    //---------------------------------------------------
    // MediaQueires Break Point Test
    //---------------------------------------------------

    //enquire.register("screen and (max-width:650px)", {
    //    match: function () {
    //        $debug.text('Small View');
    //        _narrow = false;
    //        _large = false;

    //        mobileCal();

    //        $('.site-header-narrow').find('.header-nav').removeAttr('style');

    //        if (_sLoaded === false) {
    //            $.getJSON('/handlers/laseru/navigation.ashx', { pID: $('#hdnPageID').val() }, function (headerz) {
    //            }).success(function (headerz) {
    //                SmallView(headerz);
                
    //                $(document).loadAjaxSwapImages({
    //                    images: $('img.ress'),
    //                    size: 'small'
    //                });
    //            });

    //            _sLoaded = true;
    //        }
    //    }
    //}, true).register("screen and (min-width:651px) and (max-width:850px)", {
    //    match: function () {
    //        $debug.text('Narrow View');
    //        _small = false;
    //        _large = false;

    //        $('.site-header-small').find('.header-nav').removeAttr('style');

    //        if (_nLoaded === false) {
    //            $.getJSON('/handlers/laseru/navigation.ashx', { pID: $('#hdnPageID').val() }, function (headerz) {
    //            }).success(function (headerz) {
    //                NarrowView(headerz);

    //                $(document).loadAjaxSwapImages({
    //                    images: $('img.ress'),
    //                    size: 'narrow'
    //                });
    //            });

    //            _nLoaded = true;
    //        }

    //    }
    //}, true).register("screen and (min-width:851px)", {
    //    match: function () {
    //        $debug.text('Large View');
    //        _small = false;
    //        _narrow = false;

    //        $('.site-header-small').find('.header-nav').removeAttr('style');
    //        $('.site-header-narrow').find('.header-nav').removeAttr('style');

    //        bigCal();

    //        if (_lLoaded === false) {
    //            $.getJSON('/handlers/laseru/navigation.ashx', { pID: $('#hdnPageID').val() }, function (headerz) {
    //            }).success(function (headerz) {
    //                LargeView(headerz);

    //                $(document).loadAjaxSwapImages({
    //                    images: $('img.ress'),
    //                    size: 'large'
    //                });
    //            });

    //            _lLoaded = true;
    //        }
    //    }
    //}, true).listen(250);
    
    RelatedProductsScroll();
    CheckyCheckboxerz();



    //---------------------------------------------------
    // Accordion/Filter
    //---------------------------------------------------
    $('.accordionContent').hide();

    $('.accordionHeader').on('click', function () {
        var $this = $(this);

        if ($this.next('.accordionContent').is(':visible')) {
            $this.next('.accordionContent').slideUp('fast', function () {
                $this.children('.accordionToggle').children('img').attr('src', '/images/accordionToggle_Plus.jpg');
            });
        } else {
            $this.next('.accordionContent').slideDown('fast', function () {
                $this.children('.accordionToggle').children('img').attr('src', '/images/accordionToggle_Minus.jpg');

                var maxHeight = 0;

                $this.next('.accordionContent').children('.filterRow').each(function () {

                    var $this = $(this);

                    $this.children('.filterType').each(function () {
                        if ($(this).height() > maxHeight) {
                            maxHeight = $(this).height();
                        }
                        $(this).height(maxHeight);
                    });
                });
            });
        }
    });

    $('.filterToggle img').on('click', function () {
        if ($('.filterWrap').is(':visible')) {
            $('.accordionContent').slideUp('fast', function () {
                $('.filterWrap').slideUp('slow');
            });
        } else {
            $('.filterWrap').slideDown('slow');
        }
    });

    $('.theFilters ul').each(function () {
        var $this = $(this);
        if ($this.children('li').length >= 6) {
            var theLis = $this.children('li');
            theCount = theLis.length,
			theHalf = theCount / 2,
			newRow = $this.children('li').slice(theHalf);

            $this.parent('.theFilters').append('<ul class="col2"></ul></div>');
            $this.siblings('.col2').append(newRow);
        }
    });
	
	
	//$('.filterReset a').on('click', function(){
	//	$('.accordionWrap input[type="checkbox"]').prop('checked', false);
	//});


    //---------------------------------------------------
    // Silly form alignment
    //---------------------------------------------------

    function formAlignment(theBase) {
		
		if (!$('html').hasClass('lt-ie10')) {
		
			//Run function on each first label
			$('li.form-item label:first-child').each(function () {
				var $this = $(this),
	
					//Count number of lines
					lineResult = $this.height() / parseInt($this.css('line-height'));
				
				// If more than one line, multiply number of lines and
				// theBase and use result as new top margin
				if (lineResult > 1) {
					var newMargin = theBase * lineResult + '%';
					$this.siblings('input, select, .selectFix').css({ 'margin-top': newMargin });
				} else {
					//reset margin if label now only takes up one line
					$this.siblings('input, select, .selectFix').css({ 'margin-top': 0 });
				}
			});
		}
    }//End formAlignment

    // If there are form items on the page,
    // run the formAlignment function.
    // Include a value to multiply for margins.

    if ($('.form-item').length) {
        var resizeTimer;
        formAlignment(1.55);

        // Function needs to run on resize as well
        // Use timer to make sure it runs once window is done moving.
        $(window).resize(function () {
            clearTimeout(resizeTimer);

            resizeTimer = setTimeout(function () {
                formAlignment(1.55);
            }, 100);
        });
    }
    
	var shimmerTimer;
	
    //---------------------------------------------------
    // Shimmer
    //---------------------------------------------------
//$('a.header-tagline span').shimmerText(5, 100, 5000, '#99cc33', 'white', 'true');
    (function ($) {
        $.fn.shimmerText = function (theDelay, annDelay, timerDelay, firstColor, secondColor, embed) {
			
            if (embed == 'true') {
                //append jQueryUI
                $('head').append('<script src="//code.jquery.com/ui/1.10.3/jquery-ui.js"></script>');
            }
			
            //Wrap each char in a span with a shimmer class
            $(this).contents().each(function () {
                if (this.nodeType == 3) {
                    var $this = $(this);
                    $this.replaceWith($this.text().replace(/(\w)/g, '<span class="shimmer">$&</span>'));
                }
            });
			
            //Add a index class
            $('span.shimmer').each(function (index, el) {
                $(el).addClass('idx' + index);
            });
			
            //Run the animation on a timer
            var shimmerTimer = setInterval(function () {
                $('span.shimmer').each(function (index, el) {
                    //var charCount = $('span.shimmer').length-1;
                    //console.log(charCount);
                    $(this).delay((index++) * theDelay).animate({ 'color': firstColor }, annDelay).delay((index++) * theDelay).animate({ 'color': secondColor }, annDelay, function () {
						
                        //Move the  arrow
                        if ($(this).hasClass('idx50')) {
							
                            var $this = $(this).parent('span'),
                                waitVal = 50;
								
                            $this.addClass('sillyBgFixr0').wait(waitVal, function () {
                                var $this = $(this);
                                $this.addClass('sillyBgFixr1').wait(waitVal, function () {
                                    $this.addClass('sillyBgFixr2').wait(waitVal, function () {
                                        $this.addClass('sillyBgFixr3').wait(waitVal, function () {
                                            $this.addClass('sillyBgFixr4').wait(waitVal, function () {
                                                $this.removeClass('sillyBgFixr4').wait(waitVal, function () {
                                                    $this.removeClass('sillyBgFixr3').wait(waitVal, function () {
                                                        $this.removeClass('sillyBgFixr2').wait(waitVal, function () {
                                                            $this.removeClass('sillyBgFixr1').wait(waitVal, function () {
                                                                $this.removeClass('sillyBgFixr0').wait(waitVal, function () {
                                                                });//-0
                                                            });//-1
                                                        });//-2
                                                    });//-3
                                                });//-4
                                            });//4
                                        });//3
                                    });//2
                                });//1
                            });//0
                        }//End if
                    });
                });
            }, timerDelay);
        };
    })(jQuery);





    //---------------------------------------------------
    // ShowCart
    //---------------------------------------------------
    $('body').on('click', '.header-cart-link', function (e) {
        e.preventDefault();
        $('.showCart').fadeIn('fast');
		$('.header-cart').addClass('cartOpen');
    });
    $('body').on('click', '.closeIt', function () {
        $('.showCart').fadeOut('fast');
		$('.header-cart').removeClass('cartOpen');
    });

    //Remove Item
    $('body').on('click', 'span.removeProduct', function () {
        $(this).parents('tr').fadeOut('fast', function () {
            $(this).remove();
        });
    });
    
	$('.buy-product-now').on('click', function(){
		$('.header-cart').addClass('cartOpen');
	});
	
});// end jQuery

function showSubNav() {
    var $this = $(this),
        _nav = $this.closest('li').data('nav'),
        openImage = 'url("/images/icon_small-grey-arrow-left.png") no-repeat scroll right 11px transparent',
        closedImage = 'url("/images/icon_small-grey-arrow.png") no-repeat scroll right 11px transparent';

    $this.children('a').children('span').addClass('open');
    $this.siblings('li').children('a').children('span').removeClass('open');

    if (_subnavopen === false) {
        _navtemp = _nav;

        $maincontainer.addClass('subnav-is-open');
        $('.subnav-is-open .site-subnav').animate({ 'left': '0' }, 95, function () {
            $('.subnav-is-open .content-shell').animate({ 'margin-left': '174px' }, 50);
        });

        _subnavopen = true;
        $('.site-subnav-item[data-nav="' + _nav + '"]').fadeIn('slow');

    } else {
        if (_nav === _navtemp) {
            _subnavopen = false;

            $('.site-subnav').animate({ 'left': '-410px' }, 100);
            $('.content-shell').animate({ 'margin-left': '0' }, 100);
            $('.site-subnav-item').hide().removeAttr('style');

            $maincontainer.removeClass('subnav-is-open');

            $('.header-nav-parent').children('a').children('span').removeClass('open');
            $(this).siblings('li').children('a').children('span').removeClass('open');

        } else {
            $('.site-subnav-item').hide();
            $('.site-subnav-item[data-nav="' + _nav + '"]').fadeIn('slow');

            _navtemp = _nav;
        }
    }
}

//---------------------------------------------------
// Wait Timer
//---------------------------------------------------

$.fn.wait = function( ms, callback ) {
    return this.each(function() {
        window.setTimeout((function( self ) {
            return function() {
                callback.call( self );
            };
        }( this )), ms );
    });
};

//---------------------------------------------------
// Small View
//---------------------------------------------------
function SmallView(data) {
    smallHeader = Handlebars.compile($('#smallHeader').html());
    $('.page-container').prepend(smallHeader(data));

    HeaderNav('.site-header-small');
	//$('.header-wrapper a.header-tagline').removeClass('narrow');
};

//---------------------------------------------------
// Narrow View
//---------------------------------------------------
function NarrowView(data) {
    narrowHeader = Handlebars.compile($('#narrowHeader').html());
    $('.page-container').prepend(narrowHeader(data));

    HeaderNav('.site-header-narrow');
	
	//$('.header-wrapper a.header-tagline').addClass('narrow');
	//$('.header-wrapper a.header-tagline.narrow span').shimmerText(5, 100, 5000, '#99cc33', 'white', 'true');
	
};

//---------------------------------------------------
// Large View
//---------------------------------------------------
function LargeView(data) {

    largeHeader = Handlebars.compile($('#largeHeader').html());
    $('.page-container').prepend(largeHeader(data.header));

    mainNavTemplate = Handlebars.compile($('#largeMainNav').html());
    $('section.content-navigation').append(mainNavTemplate(data.mainNav));

    subscribsTemplate = Handlebars.compile($('#subscribsTemplate').html());
    $('section.content-navigation').append(subscribsTemplate(data.subscribs));

    slideOutTemplate = Handlebars.compile($('#largeSlideOutNav').html());
    $('section.content-container .site-subnav').append(slideOutTemplate(data.mainNav));

    largeSearchTemplate = Handlebars.compile($('#largeSiteSearch').html());
    $('section.content-navigation').append(largeSearchTemplate(data.search));

    quickLinksTemplate = Handlebars.compile($('#quickLinks').html());
    $('section.content-navigation').append(quickLinksTemplate(data.quickLinks));

    secondaryNavTemplate = Handlebars.compile($('#secondaryNavigation').html());
    $('section.content-navigation').append(secondaryNavTemplate(data));

    secondaryNavTemplate = Handlebars.compile($('#smallAds').html());
    $('section.content-navigation').append(secondaryNavTemplate(data));

    $('a.header-tagline span').shimmerText(5, 100, 5000, '#99cc33', 'white', 'true');


    //$('.site-main-nav-link').each(function () {
    //    var $this = $(this);
    //});

    //$('.site-subnav-item').each(function (idx) {
    //    var $subnav = $(this);
    //    var _nav = $subnav.data('nav');

    //    $('.site-main-nav-link').each(function () {
    //        var _group = $(this).data('nav');

    //        if (_nav === _group) {
    //       //     $(this).addClass('header-nav-parent');
    //        }
    //    });
    //});

    $('#webinars').on({
        'touchstart click': function (event) {
            if (event.handled === false) return
            event.stopPropagation();
            event.preventDefault();
            event.handled = true;

            
        }
    });
    
    $('section.content-navigation').on('click', '.site-main-nav  .hasSubNav', function (e) {
        var $this = $(this),
			_nav = $this.closest('li').data('nav'),
			openImage = 'url("/presentation/laseru/images/icon_small-grey-arrow-left.png") no-repeat scroll right 11px transparent',
			closedImage = 'url("/presentation/laseru/images/icon_small-grey-arrow.png") no-repeat scroll right 11px transparent';

        $this.children('a').children('span').addClass('open');
        $this.siblings('li').children('a').children('span').removeClass('open');

        if (_subnavopen === false) {
            _navtemp = _nav;

            $maincontainer.addClass('subnav-is-open');
            $('.subnav-is-open .site-subnav').animate({ 'left': '0' }, 95, function () {
                $('.subnav-is-open .content-shell').animate({ 'margin-left': '174px' }, 50);
            });

            _subnavopen = true;
            $('.site-subnav-item[data-nav="' + _nav + '"]').fadeIn('slow');

        } else {
            if (_nav === _navtemp) {
                _subnavopen = false;

                $('.site-subnav').animate({ 'left': '-410px' }, 100);
                $('.content-shell').animate({ 'margin-left': '0' }, 100);
                $('.site-subnav-item').hide().removeAttr('style');

                $maincontainer.removeClass('subnav-is-open');

                $('.header-nav-parent').children('a').children('span').removeClass('open');
                $(this).siblings('li').children('a').children('span').removeClass('open');

            } else {
                $('.site-subnav-item').hide();
                $('.site-subnav-item[data-nav="' + _nav + '"]').fadeIn('slow');

                _navtemp = _nav;
            }
        }

        e.preventDefault();
    });
};

//---------------------------------------------------
// Event Calendar
//---------------------------------------------------
function bigCal(){
	$('.popIt').off();
	$('.day a.popIt').on('click', function(e){
		e.preventDefault();
		$(this).siblings('.dayPop').fadeIn('fast');
	});
	$('.closeIt').on('click', function(){
		$(this).parent('.dayPop').fadeOut('slow');
	});
}//bigCal

function mobileCal(){
	$('.popIt').off().each(function(){
		var newHash = $(this).siblings('h4').text();	
		$(this).attr('href', '#'+ newHash);
	});
	$('.dayNumber').each(function(){
		var theID = $(this).text();
		$(this).parent('dd').parent('dl').siblings('h2.listing-item-h2').attr('id', theID);
	});
	$('.popIt').on('click', function(e){
		e.preventDefault();
		var thisHash = $(this).attr('href'),
			hashPos = $(thisHash).offset();
		$('html, body').animate({scrollTop: hashPos.top}, 'slow');
	});
}//mobileCal


//---------------------------------------------------
// Related Products Scrolly
//---------------------------------------------------
function RelatedProductsScroll() {
	$('.btn-related-products').off();
	$('.btn-related-products').on('click', function(e) {		
		$("html, body").stop().animate({ scrollTop: $('#related-products').offset().top }, 800);		
		e.preventDefault();
	});
}


//---------------------------------------------------
// Checkboxerz Replacement
//---------------------------------------------------
function CheckyCheckboxerz() {
	$('<a href="#" class="checkbox-link">x</a>').insertAfter('.form-item input:checkbox');
	$('input:checked').parent().addClass('checked');
	
	$('.checkbox-link').off();
	$('.checkbox-link').on('click', function(e) {
		
		if ($(this).parent('span').hasClass('checked')) {
			$(this).parent('span').removeClass('checked');
			$(this).siblings('input:checkbox').prop('checked', false);
		}else{
			$(this).parent('span').addClass('checked');
			$(this).siblings('input:checkbox').prop('checked', true);
		}
		
		e.preventDefault();
	});
}


    jQuery(window).scroll(function() {
        if(jQuery(this).scrollTop() != 0) { // Если до верха не 0px
            jQuery('#toTop').fadeIn();  // Плавно показываем кнопочку :)
        } else {                            // Иначе (Если мы вверху страницы) 
            jQuery('#toTop').fadeOut(); // Столь же плавно эту самую кнопочку скрываем
        }
    });
 
    jQuery('#toTop').click(function() {
        jQuery('body,html').animate({scrollTop:0},800);
    });   


//---------------------------------------------------
// Header Nav
//---------------------------------------------------
function HeaderNav(headerWrap) {
    var $container = $('.page-container');

    var $headerlink = $(headerWrap).find('.header-nav-item');
    $headerlink.each(function () {
        var $this = $(this);
        $this.has('ul').addClass('header-nav-parent').find('a');
    });

    $('<span class="header-nav-expand" />').insertAfter($(headerWrap).find('.header-nav-parent > a'));

    $container.on('click', headerWrap + ' .header-nav-expand', function (e) {
        var $this = $(this);
        $this.toggleClass('header-nav-expanded').closest('li').toggleClass('header-hav-parent-expanded').find('ul').slideToggle(200);
    });

    var $menutoggle = $(headerWrap).find('.header-nav-toggle');
    $(headerWrap).find('.header-nav-toggle').off();

    $container.on('click', headerWrap+ ' .header-nav-toggle', function (e) {
        $('.header-nav').slideToggle(500);
        $(headerWrap).toggleClass('header-nav-opened');
        e.preventDefault();
    });

    var $searchtoggle = $(headerWrap).find('.header-search-toggle');
    $(headerWrap).find('.header-search-toggle').off();

    $container.on('click', headerWrap +' .header-search-toggle', function (e) {
        $(headerWrap).toggleClass('header-search-opened');
        $searchtoggle.toggleClass('header-search-toggle-active');
        $(headerWrap).find('.header-advanced-search').slideToggle(200);
        e.preventDefault();
    });
}