//-----------------------------------------
// Laser Therapy Univeristy
// 
// Last Updated By: Ryan Perry
// Last Updated: 06/17/2013
//-----------------------------------------

jQuery(function($) {

if(Modernizr.mq('only screen and (max-width:650px)')) {

}else if(Modernizr.mq('only screen and (min-width:651px)')) {
	LoadSharing();
} else {
	LoadSharing();
}

enquire.register("screen and (min-width:651px)", {
	match : function() {
		LoadSharing();
	}
}, true).listen(250);


});


function LoadSharing() {
	Modernizr.load([
	{
		load: 'js/socialite.js',
		complete: function () {				
			Socialite.load();
		}
	}]);
}