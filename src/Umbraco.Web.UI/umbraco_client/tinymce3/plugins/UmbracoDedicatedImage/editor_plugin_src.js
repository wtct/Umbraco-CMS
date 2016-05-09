/**
 * $Id: editor_plugin_src.js 677 2008-03-07 13:52:41Z spocke $
 *
 * @author Moxiecode
 * @copyright Copyright © 2004-2008, Moxiecode Systems AB, All rights reserved.
 */

(function() {
//    tinymce.PluginManager.requireLangPack('umbraco');

    tinymce.create('tinymce.plugins.UmbracoDedicatedImagePlugin', {
        init: function(ed, url) {
            // Register commands
            ed.addCommand('mceUmbDedicatedImage', function() {
                // Internal image object like a flash placeholder
                if (ed.dom.getAttrib(ed.selection.getNode(), 'class').indexOf('mceItem') != -1)
                    return;

                ed.windowManager.open({
                    /* UMBRACO SPECIFIC: Load Umbraco modal window */
					file: tinyMCE.activeEditor.getParam('umbraco_path') + '/plugins/tinymce3/insertDedicatedImage.aspx?umbPageId=' + tinyMCE.activeEditor.getParam('theme_umbraco_pageId'),
                    width: 575 + ed.getLang('umbracoimg.delta_width', 0),
                    height: 505 + ed.getLang('umbracoimg.delta_height', 0),
                    inline: 1
                }, {
                    plugin_url: url
                });
            });

            // Register buttons
            ed.addButton('UmbracoDedicatedImage', {
                title: 'Insert/edit dedicated image',
                cmd: 'mceUmbDedicatedImage',
                image: tinyMCE.activeEditor.getParam("umbraco_path") + "/images/editor/dedicated_image.gif"
            });

        },

        getInfo: function() {
            return {
                longname: 'Umbraco dedicated image dialog',
                author: 'Umbraco and wtct',
                authorurl: 'http://umbraco.org',
                infourl: 'http://umbraco.org',
                version: "1.0"
            };
        }
    });

    // Register plugin
    tinymce.PluginManager.add('UmbracoDedicatedImage', tinymce.plugins.UmbracoDedicatedImagePlugin);
    
})();