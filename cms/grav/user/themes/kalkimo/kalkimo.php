<?php
namespace Grav\Theme;

use Grav\Common\Theme;

class Kalkimo extends Theme
{
    public static function getSubscribedEvents(): array
    {
        return [
            'onThemeInitialized' => ['onThemeInitialized', 0],
        ];
    }

    public function onThemeInitialized(): void
    {
        if ($this->isAdmin()) {
            return;
        }

        $this->enable([
            'onTwigSiteVariables' => ['onTwigSiteVariables', 0],
        ]);
    }

    public function onTwigSiteVariables(): void
    {
        $assets = $this->grav['assets'];

        // CSS
        $assets->addCss('theme://css/variables.css', ['priority' => 100]);
        $assets->addCss('theme://css/theme.css', ['priority' => 99]);
        $assets->addCss('theme://css/components/header.css', ['priority' => 98]);
        $assets->addCss('theme://css/components/footer.css', ['priority' => 97]);
        $assets->addCss('theme://css/components/hero.css', ['priority' => 96]);
        $assets->addCss('theme://css/components/cards.css', ['priority' => 95]);
        $assets->addCss('theme://css/components/forms.css', ['priority' => 94]);
        $assets->addCss('theme://css/components/utilities.css', ['priority' => 93]);

        // JS
        $assets->addJs('theme://js/main.js', ['group' => 'bottom', 'defer' => true]);
    }
}
