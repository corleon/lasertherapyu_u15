
jQuery(document).ready(function($) {

var bV = '536.29.13';

if(Modernizr.cssfilters) {
	$('html').addClass('css-blur');
	//$('html').addClass('no-css-blur');
	
}


var $modal = $('div#modal');

$modal.show('fast', function() {
	
	//$(document).bind("contextmenu",function(e){
	//	return false;
	//}); 
	
	
	$('input.modal-button').on('click', function() {
		$modal.hide('fast', function() {
			$('div.page-container').removeClass('blur');
			$('#overlay').fadeOut('slow');
		});
		
	});
	  
});
	


// end jQuery	
});