<?php
namespace Grav\Plugin;

use Grav\Common\Plugin;
use RocketTheme\Toolbox\Event\Event;

/**
 * Kalkimo Embed Plugin
 *
 * Detects <km-planner> custom elements in page content
 * and injects the Vue.js embed bundle assets.
 */
class KalkimoEmbedPlugin extends Plugin
{
    public static function getSubscribedEvents(): array
    {
        return [
            'onPluginsInitialized' => ['onPluginsInitialized', 0],
        ];
    }

    public function onPluginsInitialized(): void
    {
        if ($this->isAdmin()) {
            return;
        }

        $this->enable([
            'onPageContentProcessed' => ['onPageContentProcessed', 0],
        ]);
    }

    public function onPageContentProcessed(Event $event): void
    {
        $page = $event['page'];
        $content = $page->getRawContent();

        if (strpos($content, '<km-planner') === false) {
            return;
        }

        $config = $this->grav['config'];
        $assets = $this->grav['assets'];

        $jsPath = $config->get('plugins.kalkimo-embed.embed_js',
            'plugin://kalkimo-embed/assets/km-planner.embed.js');
        $cssPath = $config->get('plugins.kalkimo-embed.embed_css',
            'plugin://kalkimo-embed/assets/km-planner.embed.css');

        $assets->addJs($jsPath, ['group' => 'bottom', 'defer' => true]);
        $assets->addCss($cssPath);
    }
}
