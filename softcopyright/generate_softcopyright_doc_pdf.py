#!/usr/bin/env python3
"""
软件著作权登记 - 文档鉴别材料（设计说明书）PDF 生成工具
项目: 墨字防线小游戏软件

截图未放入 softcopyright/pics 时，PDF 中自动生成「待补截图」占位框。
"""

import warnings
from pathlib import Path

from fpdf import FPDF
from fpdf.enums import WrapMode
from PIL import Image

warnings.filterwarnings("ignore", category=DeprecationWarning)


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = Path('/Users/huyi/dk_proj/black-rosa')
OUTPUT = SCRIPT_DIR / '软著文档-墨字防线-V1.0.0.pdf'

SOFTWARE_FULL_NAME = '深圳幸运呱科技有限公司墨字防线小游戏软件'
SOFTWARE_VERSION = 'V1.0.0'
APPLICANT_NAME = '深圳幸运呱科技有限公司'

SONGTI_PATH = '/System/Library/Fonts/Supplemental/Songti.ttc'

BODY_FONT_SIZE = 10.5
H1_FONT_SIZE = 16
H2_FONT_SIZE = 14
H3_FONT_SIZE = 12
CODE_FONT_SIZE = 9
HEADER_FONT_SIZE = 10
FOOTER_FONT_SIZE = 9

LINE_HEIGHT = 6.5
CODE_LINE_HEIGHT = 5.0
H1_LINE_HEIGHT = 10
H2_LINE_HEIGHT = 8.5
H3_LINE_HEIGHT = 7.5

LEFT_MARGIN = 25
RIGHT_MARGIN = 20
TOP_MARGIN = 15
BOTTOM_MARGIN = 15

PAGE_W = 210
PAGE_H = 297
CONTENT_W = PAGE_W - LEFT_MARGIN - RIGHT_MARGIN
CONTENT_TOP = TOP_MARGIN + 10

HEADER_TEXT = f'{SOFTWARE_FULL_NAME} {SOFTWARE_VERSION} 设计说明书'
PICS_DIR = SCRIPT_DIR / 'pics'


def _pic(*names):
    for name in names:
        path = PICS_DIR / name
        if path.exists():
            return path
    return PICS_DIR / names[0]


SCREENSHOTS = {
    'loading': [(_pic('ink_17_loading.jpg', 'ink_17_loading.png'),
                 '图1  加载页 - 游戏名称、进度与健康游戏忠告')],
    'home': [(_pic('ink_01_home.jpg', 'ink_01_home.png'),
              '图2  首页出征页 - 体力、墨、钻石、章节关卡与底栏')],
    'battle': [(_pic('ink_02_battle.jpg', 'ink_02_battle.png'),
                '图3  战斗界面 - 炮串、字格、敌人、改装键与道具冷却')],
    'draft': [(_pic('ink_03_draft.jpg', 'ink_03_draft.png'),
               '图4  改装抽屉 - 三选一，不挡住上方棋盘')],
    'place': [(_pic('ink_04_place.jpg', 'ink_04_place.png'),
               '图5  放置与升星 - 空格放下或同名叠星')],
    'chest': [(_pic('ink_05_chest.jpg', 'ink_05_chest.png'),
               '图6  战场宝箱 - 限时金币箱或墨箱')],
    'item_cast': [(_pic('ink_15_item_cast.jpg', 'ink_15_item_cast.png'),
                   '图7  道具自动触发 - 冷却好后自己放出')],
    'forge': [(_pic('ink_06_forge.jpg', 'ink_06_forge.png'),
               '图8  炮台页 - 皮肤展台与成长词条')],
    'item': [(_pic('ink_07_item.jpg', 'ink_07_item.png'),
              '图9  道具页 - 三个装备槽与九张道具卡')],
    'victory': [(_pic('ink_08_victory.jpg', 'ink_08_victory.png'),
                 '图10  通关结算 - 星级、入账的墨、钻石与通关宝箱')],
    'revive': [(_pic('ink_09_revive.jpg', 'ink_09_revive.png'),
                '图11  续命 - 失败后可看视频恢复战斗')],
    'defeat': [(_pic('ink_10_defeat.jpg', 'ink_10_defeat.png'),
                '图12  失败页 - 完成度与回首页')],
    'chest_open': [(_pic('ink_11_chest_open.jpg', 'ink_11_chest_open.png'),
                    '图13  通关宝箱开启 - 墨与道具卡')],
    'checkin': [(_pic('ink_12_checkin.jpg', 'ink_12_checkin.png'),
                 '图14  七日签到 - 每天奖励与第 7 天宝箱')],
    'gift': [(_pic('ink_18_gift.jpg', 'ink_18_gift.png'),
              '图15  新手礼包 - 看两次广告领银宝箱、钻石和墨')],
    'club': [(_pic('ink_16_club.jpg', 'ink_16_club.png'),
              '图16  游戏圈奖励 - 发帖领取墨和木宝箱')],
    'codex': [(_pic('ink_13_codex.jpg', 'ink_13_codex.png'),
               '图17  图鉴 - 字谱、秘卷或墨谱')],
    'rank': [(_pic('ink_14_rank.jpg', 'ink_14_rank.png'),
              '图18  排行榜 - 按已通关关数排序')],
}


class DocPDF(FPDF):
    def __init__(self):
        super().__init__(orientation='P', unit='mm', format='A4')
        self.set_left_margin(LEFT_MARGIN)
        self.set_right_margin(RIGHT_MARGIN)
        self.set_top_margin(CONTENT_TOP)
        self.set_auto_page_break(auto=True, margin=BOTTOM_MARGIN + 10)
        self.missing_images = []

    def header(self):
        self.set_font('Songti', '', HEADER_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_xy(LEFT_MARGIN, TOP_MARGIN)
        self.cell(0, 6, HEADER_TEXT, new_x='LEFT', new_y='TOP')
        page_str = str(self.page_no())
        tw = self.get_string_width(page_str)
        self.set_xy(PAGE_W - RIGHT_MARGIN - tw, TOP_MARGIN)
        self.cell(tw, 6, page_str, new_x='LEFT', new_y='TOP')
        line_y = TOP_MARGIN + 7
        self.set_draw_color(0, 0, 0)
        self.set_line_width(0.4)
        self.line(LEFT_MARGIN, line_y, PAGE_W - RIGHT_MARGIN, line_y)
        self.set_y(CONTENT_TOP)

    def footer(self):
        footer_y = PAGE_H - BOTTOM_MARGIN
        self.set_xy(LEFT_MARGIN, footer_y)
        self.set_font('Songti', '', FOOTER_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.cell(CONTENT_W, 5, APPLICANT_NAME, align='C')

    def check_page_break(self, h):
        if self.get_y() + h > PAGE_H - BOTTOM_MARGIN - 10:
            self.add_page()

    def write_h1(self, text):
        self.check_page_break(H1_LINE_HEIGHT + 5)
        self.ln(4)
        self.set_font('Songti', '', H1_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_x(LEFT_MARGIN)
        self.cell(CONTENT_W, H1_LINE_HEIGHT, _safe_text(text), new_x='LMARGIN', new_y='NEXT')
        self.ln(2)

    def write_h2(self, text):
        self.check_page_break(H2_LINE_HEIGHT + 4)
        self.ln(3)
        self.set_font('Songti', '', H2_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_x(LEFT_MARGIN)
        self.cell(CONTENT_W, H2_LINE_HEIGHT, _safe_text(text), new_x='LMARGIN', new_y='NEXT')
        self.ln(1.5)

    def write_h3(self, text):
        self.check_page_break(H3_LINE_HEIGHT + 3)
        self.ln(2)
        self.set_font('Songti', '', H3_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_x(LEFT_MARGIN)
        self.cell(CONTENT_W, H3_LINE_HEIGHT, _safe_text(text), new_x='LMARGIN', new_y='NEXT')
        self.ln(1)

    def write_body(self, text, indent=0):
        self.set_font('Songti', '', BODY_FONT_SIZE)
        self.set_text_color(30, 30, 30)
        self.set_x(LEFT_MARGIN + indent)
        self.multi_cell(CONTENT_W - indent, LINE_HEIGHT, _safe_text(text),
                        new_x='LMARGIN', new_y='NEXT', wrapmode=WrapMode.CHAR)

    def write_bullet(self, text, level=0):
        indent = 4 + level * 4
        bullet = '  ' * level + ('- ' if level > 0 else '* ')
        self.write_body(bullet + text, indent=indent)

    def write_code_block(self, lines):
        self.ln(1)
        self.set_font('Songti', '', CODE_FONT_SIZE)
        self.set_text_color(40, 40, 40)
        for line in lines:
            self.check_page_break(CODE_LINE_HEIGHT)
            self.set_fill_color(245, 245, 245)
            self.set_x(LEFT_MARGIN + 4)
            self.cell(CONTENT_W - 4, CODE_LINE_HEIGHT, _safe_text(line.replace('\t', '    ')),
                      fill=True, new_x='LMARGIN', new_y='NEXT')
        self.ln(1)

    def write_table(self, headers, rows, col_widths=None):
        self.ln(1)
        if col_widths is None:
            col_widths = [CONTENT_W / len(headers)] * len(headers)
        row_line_h = 5.6
        pad_x = 1.5
        pad_y = 1.5

        def wrap_cell(text, width):
            text = _safe_text(str(text))
            lines = []
            for paragraph in text.split('\n'):
                current = ''
                for ch in paragraph:
                    if self.get_string_width(current + ch) <= width:
                        current += ch
                    else:
                        if current:
                            lines.append(current)
                        current = ch
                lines.append(current)
            return lines or ['']

        def draw_row(cells, fill):
            self.set_font('Songti', '', BODY_FONT_SIZE)
            wrapped = [wrap_cell(c, col_widths[i] - pad_x * 2) for i, c in enumerate(cells)]
            row_h = max(len(lines) for lines in wrapped) * row_line_h + pad_y * 2
            self.check_page_break(row_h)
            y0 = self.get_y()
            x = LEFT_MARGIN
            self.set_fill_color(*fill)
            self.set_draw_color(0, 0, 0)
            for i, lines in enumerate(wrapped):
                self.rect(x, y0, col_widths[i], row_h, style='DF')
                self.set_xy(x + pad_x, y0 + pad_y)
                for line in lines:
                    self.cell(col_widths[i] - pad_x * 2, row_line_h, line,
                              new_x='LEFT', new_y='NEXT')
                    self.set_x(x + pad_x)
                x += col_widths[i]
            self.set_y(y0 + row_h)

        self.set_font('Songti', '', BODY_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        draw_row(headers, (230, 230, 230))
        for row in rows:
            self.set_text_color(30, 30, 30)
            draw_row(row, (255, 255, 255))
        self.ln(1)

    def write_image(self, img_path, caption='', max_h=90):
        img_path = Path(img_path)
        if not img_path.exists():
            self._write_image_placeholder(img_path, caption)
            return
        img = Image.open(img_path)
        iw, ih = img.size
        max_w = CONTENT_W * 0.48
        ratio = min(max_w / iw, max_h / ih)
        draw_w = iw * ratio
        draw_h = ih * ratio
        self.check_page_break(draw_h + 18)
        self.ln(3)
        x = LEFT_MARGIN + (CONTENT_W - draw_w) / 2
        self.image(str(img_path), x=x, y=self.get_y(), w=draw_w, h=draw_h)
        self.set_y(self.get_y() + draw_h + 2)
        if caption:
            self.set_font('Songti', '', 9)
            self.set_text_color(100, 100, 100)
            self.set_x(LEFT_MARGIN)
            self.cell(CONTENT_W, 5, _safe_text(caption), align='C', new_x='LMARGIN', new_y='NEXT')
            self.set_text_color(30, 30, 30)
        self.ln(3)

    def _write_image_placeholder(self, img_path, caption):
        self.missing_images.append((str(img_path), caption))
        box_h = 58
        self.check_page_break(box_h + 16)
        self.ln(3)
        x = LEFT_MARGIN + CONTENT_W * 0.22
        w = CONTENT_W * 0.56
        y = self.get_y()
        self.set_draw_color(120, 120, 120)
        self.set_fill_color(248, 248, 248)
        self.rect(x, y, w, box_h, style='DF')
        self.set_font('Songti', '', 11)
        self.set_text_color(120, 120, 120)
        self.set_xy(x, y + 18)
        self.cell(w, 7, '待补游戏截图', align='C', new_x='LEFT', new_y='NEXT')
        self.set_xy(x, y + 28)
        self.cell(w, 7, img_path.name, align='C', new_x='LEFT', new_y='NEXT')
        self.set_y(y + box_h + 2)
        self.set_font('Songti', '', 9)
        self.set_text_color(100, 100, 100)
        self.set_x(LEFT_MARGIN)
        self.cell(CONTENT_W, 5, _safe_text(caption + '（截图占位）'), align='C',
                  new_x='LMARGIN', new_y='NEXT')
        self.set_text_color(30, 30, 30)
        self.ln(3)


def _safe_text(text):
    replacements = {
        '→': '->', '←': '<-', '↑': '^', '↓': 'v', '★': '*',
        '“': '"', '”': '"', '‘': "'", '’': "'",
        '—': '-', '·': '.', '：': ':', '（': '(', '）': ')',
    }
    for old, new in replacements.items():
        text = text.replace(old, new)
    return ''.join(c if ord(c) <= 0xFFFF else '?' for c in text)


def img(key):
    return SCREENSHOTS[key][0]


def write_document(pdf):
    pdf.add_page()
    pdf.write_h1('目  录')
    for item in [
        '一、引言',
        '    1.1 编写目的',
        '    1.2 软件概述',
        '    1.3 运行环境',
        '    1.4 术语与缩略语',
        '二、软件总体设计',
        '    2.1 软件需求概括',
        '    2.2 总体架构设计',
        '    2.3 模块划分与关系',
        '    2.4 场景与界面系统设计',
        '    2.5 主循环与资源加载设计',
        '三、核心模块详细设计',
        '    3.1 游戏入口与云同步启动',
        '    3.2 战斗世界与炮串移动',
        '    3.3 字牌、词组、招牌与命中结算',
        '    3.4 出怪、敌人与关卡规则',
        '    3.5 改装抽牌与放置升星',
        '    3.6 掉落、墨摊与战场宝箱',
        '    3.7 局内自动道具',
        '    3.8 首页、出征、体力与钻石',
        '    3.9 炮台成长与皮肤',
        '    3.10 道具成长与装备',
        '    3.11 结算、评星与通关宝箱',
        '    3.12 签到、礼包、游戏圈与图鉴',
        '    3.13 排行榜与云存档',
        '四、数据结构设计',
        '五、数据接口设计',
        '六、出错处理设计',
        '七、性能优化设计',
        '八、结论',
    ]:
        pdf.write_body(item)

    pdf.add_page()
    pdf.write_h1('一、引言')
    pdf.write_h2('1.1 编写目的')
    pdf.write_body(
        f'编写本设计说明书是{SOFTWARE_FULL_NAME} {SOFTWARE_VERSION}软件著作权登记材料的一部分。'
        '本文档用于说明本软件的功能范围、总体架构、核心模块、数据结构、接口设计、异常处理和性能优化方案，'
        '证明本软件为独立开发完成的原创游戏软件。'
    )
    pdf.write_body(
        '本文档面向软件著作权审查人员及后续维护人员，重点描述软件技术实现，不包含运营数据、用户隐私数据和商业敏感策略。'
        '界面图按文件名放在 softcopyright/pics，未放入时说明书中保留占位框。'
    )

    pdf.write_h2('1.2 软件概述')
    pdf.write_body(
        '墨字防线是一款基于微信小游戏运行环境开发的竖屏塔防射击游戏。玩家用单指左右滑动底部炮串，'
        '炮弹自下而上穿过字格，按穿过顺序叠加灼烧、减速、分裂、爆炸等效果，拦截从上方涌来的敌人。'
        '击杀掉落金币与墨。金币只在本局用来抽改装。通关后本局拾到的墨记入局外，用于炮台成长、皮肤和道具升级。'
        '道具冷却结束且场面对得上时自动释放，不消耗金币。通关发放钻石和等待开启的宝箱。'
    )
    pdf.write_body(
        '启动后先进入加载页，展示游戏名称和加载进度，并在页面下方展示《健康游戏忠告》全文。加载完成后再进入首页。'
    )
    p, c = img('loading')
    pdf.write_image(p, c)
    pdf.write_body('本软件的主要功能包括:')
    for text in [
        '列式改装战斗: 六列最多三行字格，子弹穿过一列时按顺序收集字效，命中走统一结算。',
        '三选一改装: 本局金币达到抽牌价后自动暂停，从关卡牌池抽最多三张一字牌，放到空格或给同名牌升星。',
        '词组与招牌: 秒杀、万箭、击退、连斩必须同列凑齐才生效；两种字叠在同一发上可化出霜火、焦雷等形态。',
        '波次关卡: 八章共七十二关，按时间刷怪，漏怪扣基地生命，按剩余生命评一至三星。',
        '掉落与宝箱: 击杀掉金币和墨，并可能掉出限时战场箱；通关另发木、银、金或皇家宝箱。',
        '自动道具: 首页装备最多三个道具，战斗中按条件自动释放，效果与字牌结算分开。',
        '局外成长: 墨购买伤害、炮数、射速、开局金币、基地生命，以及皮肤和道具等级。',
        '本地与云端存档: 体力、通关、锻造、皮肤、道具、宝箱和钻石写入本地，并与云端按时间合并。',
    ]:
        pdf.write_bullet(text)
    p, c = img('home')
    pdf.write_image(p, c)

    pdf.write_h2('1.3 运行环境')
    pdf.write_table(
        ['环境项', '要求说明'],
        [
            ['客户端平台', '微信小游戏；编辑器内可直接运行同一套逻辑'],
            ['开发语言', '客户端 C#；云函数 JavaScript'],
            ['引擎', '团结引擎，脚本与场景按 Unity 兼容工程组织'],
            ['渲染', '运行时创建的 2D 精灵、UI Canvas 与粒子表现'],
            ['构建', '团结引擎导出微信小游戏包，再用微信开发者工具预览'],
            ['本地存储', 'PlayerPrefs，进度键名 inkline.meta.v2'],
            ['云端', '腾讯云 CloudBase 云函数 blackrosa-api，JWT 鉴权'],
            ['资源分发', '章节图、战斗背景和战斗音乐走 CDN，包内保留缩略图'],
        ],
        [32, 133],
    )

    pdf.write_h2('1.4 术语与缩略语')
    pdf.write_table(
        ['术语', '含义说明'],
        [
            ['字牌', '放在字格里的一个汉字改装，只改穿过该列的子弹'],
            ['词组', '同列凑齐的一对字，如秒与杀、万与箭，凑齐后才改弹'],
            ['招牌', '两种不同字叠在同一发上的形态，如霜火、焦雷，只改已有数值'],
            ['星', '同一格上同名字牌叠到的等级，上限 3；通关评星另按剩余生命计算'],
            ['墨', '局外货币。通关时按本局拾取量入账，也从宝箱、签到和游戏圈获得'],
            ['金币', '只在本局存在，用来抽改装，关卡结束清空。道具不花金币'],
            ['钻石', '首通、签到和礼包获得，用来加速开箱或兑换体力'],
            ['道具', '带进关的实物。冷却好了、条件满足就自动丢出，不占字格'],
            ['炮串', '底部相邻的若干门炮，整串平移并吸附到列中心'],
            ['ForgeStats', '局外成长折算后的开局条件，战斗只读这个结构'],
        ],
        [32, 133],
    )

    pdf.write_h1('二、软件总体设计')
    pdf.write_h2('2.1 软件需求概括')
    pdf.write_body(
        '本软件把操作压成一根手指，把决策放在「往哪列开火」和「字怎么叠」。'
        '道具自己按场面释放，玩家不用在走怪时再点技能。'
        '战斗模拟、界面和存档分开：BattleWorld 不创建界面对象，HomeScreen 与 BattleHud 不改命中公式。'
    )
    pdf.write_body('软件核心需求包括以下几个方面:')
    for text in [
        '提供可暂停的持续战斗。波次只负责出怪节奏，抽牌跟金币走，不跟波次绑定。',
        '提供稳定的列式改装：穿过、蓄能、成词、招牌、升星、覆盖确认。',
        '分清三套资源：本局金币、局外墨、钻石。边界不能混用。',
        '提供首页三页：出征选关、炮台成长与皮肤、道具升级与装备。',
        '提供签到、新手礼包、游戏圈、图鉴和通关排行榜。',
        '提供可靠的本地存档，并在换机时按更新时间与云端合并。旧档字段变长时补齐，不整档作废。',
    ]:
        pdf.write_bullet(text)

    pdf.write_h2('2.2 总体架构设计')
    pdf.write_body(
        '软件整体划分为入口层、流程层、战斗模拟层、配置层、界面层、表现层、平台层和云函数层。'
        '流程层持有当前屏幕状态，战斗层只推进一局模拟，界面层负责把状态画出来。'
        '云函数不参与战斗计算，只负责登录、存档、排行榜和游戏圈发帖校验。'
    )
    pdf.write_code_block([
        'GameBootstrap.Boot',
        '  +-- GameFlow                 首页 / 战斗 / 抽牌 / 放置 / 结算',
        '  +-- CloudSync                 启动拉云，存档变更后防抖上行',
        '  +-- MetaProgress              体力、墨、钻石、通关、锻造、皮肤、道具、宝箱',
        '  +-- BattleWorld               炮、子弹、敌人、字格、战场箱、自动道具',
        '  +-- StageCatalog / CardCatalog / ItemCatalog / ForgeCatalog / ChestCatalog',
        '  +-- HomeScreen / BattleHud / DraftView / VictoryPanel / RevivePanel / DefeatPanel',
        '  +-- BattleView / InkShot / InkVfx / CdnAssets',
        '  +-- WxBridge / Backend / RankService',
        '  +-- blackrosa-api             login / save / rank / gameclub',
    ])

    pdf.write_h2('2.3 模块划分与关系')
    pdf.write_table(
        ['模块层', '代表对象', '功能简述'],
        [
            ['入口层', 'GameBootstrap', '场景加载后创建唯一 GameFlow'],
            ['流程层', 'GameFlow', '切屏、扣体力、开抽牌、写结算'],
            ['模拟层', 'BattleWorld', '时间推进、命中、掉落、胜负、道具'],
            ['配置层', 'StageCatalog 等', '关卡、字、敌人、道具、锻造、宝箱'],
            ['界面层', 'HomeScreen / BattleHud', '首页三页和战斗顶栏底栏'],
            ['表现层', 'BattleView / InkVfx / ItemFx', '炮、弹、字格、墨摊和道具演出'],
            ['平台层', 'WxBridge / CloudSync / CdnAssets', '微信、云存档、CDN 和音效'],
            ['云函数', 'blackrosa-api', '登录、存档合并、排行榜、游戏圈'],
        ],
        [24, 52, 89],
    )
    pdf.write_body(
        '协作顺序是：启动时 CloudSync 先登录并决定用本地档还是云端档，然后 GameFlow 读 MetaProgress 进首页。'
        '界面把玩家操作交给 GameFlow，GameFlow 改 MetaProgress 或调用 BattleWorld，'
        '下一帧 View 读取 BattleWorld 的公开列表做同步。界面不直接改敌人血量，战斗也不直接写 PlayerPrefs。'
    )
    pdf.write_code_block([
        'WxBridge.InitSdk -> CloudSync.Startup -> MetaProgress.Load -> ShowHome',
        'HomeScreen 点继续 -> GameFlow.StartStage',
        'StartStage: SpendStamina -> BattleWorld.Begin -> 战斗开始条',
        '金币够了 -> OpenDraft -> DraftView',
        '点格子 -> BattleWorld.Place',
        'Victory -> ApplyResult -> RankService.Submit',
    ])

    pdf.write_h2('2.4 场景与界面系统设计')
    pdf.write_body(
        '工程不在编辑器里摆战斗物体。GameFlow 在运行时创建 Canvas，首页和战斗界面都挂在同一层上。'
        '屏幕状态分为首页、战斗开始、战斗、抽牌、放置、确认和结算。抽牌、确认、撤退询问和结算会暂停模拟。'
    )
    pdf.write_body(
        '首页底栏三个页签：炮台、出征、道具。出征是默认页，按章横向滑动关卡卡，一章九关排成带弯的三行。'
        '继续按钮下面是四个宝箱托盘。左侧贴纸是新手礼包、游戏圈、签到，右侧是图鉴和排行榜。'
        '顶栏显示体力和墨。战斗界面上方走敌人，中间是字格，底部是炮串；'
        '顶栏有撤退、本局金币和本局墨，底栏是改装键、道具冷却和加炮入口。'
        '改装从底部拉起抽屉，上沿停在格子下面，走着的怪和已放的字仍然看得见。'
    )
    p, c = img('battle')
    pdf.write_image(p, c)

    pdf.write_h2('2.5 主循环与资源加载设计')
    pdf.write_body(
        'GameFlow.Update 每帧做这些事：按安全区适配画布、推进音效、在战斗态读取触控并调用 BattleWorld.Tick、'
        '让 BattleView.Sync 把模拟对象对齐到精灵。金币够了且玩家没有按着炮串时，等待约 0.35 秒后自动打开改装。'
        '胜负在 Tick 之后立刻进入结算，避免同一帧重复入账。'
    )
    pdf.write_body(
        '小图从 Resources 加载。章节图、战场背景和战斗音乐放在云存储，由 CdnAssets 按清单拉取。'
        '图片先用包内缩略图顶上，高清到达后再替换；拉不到就一直用缩略图，不会留白。'
        '编辑器直接读本地源文件。内存里同时保留的高清图有上限，正在显示的不参与淘汰。'
        '字效不给每种组合单独画一张，炮弹按体、尾、环、绕、晕光五个槽叠加，招牌两两只替换体槽。'
    )

    pdf.write_h1('三、核心模块详细设计')
    pdf.write_h2('3.1 游戏入口与云同步启动')
    pdf.write_body(
        'GameBootstrap 在场景加载后执行。若场上还没有 GameFlow，就创建一个 DontDestroyOnLoad 的根物体并挂上它。'
        '同时把目标帧率设为 60，并打开触摸模拟，保证编辑器里用鼠标也能拖炮串。'
    )
    pdf.write_body(
        'GameFlow 启动后先初始化微信 SDK，再把进大厅的动作交给 CloudSync。'
        '云同步在超时时间内登录并拉取存档：云端更新更晚就覆盖本地，本地更新更晚就保留并标记待上行。'
        '登录或拉取失败时进入只读缓存，禁止把一份新空档推上去盖掉云端进度。'
        '进大厅之后才到达的云端档会重读 MetaProgress；如果当时正在战斗，只换存档，不打断这一局。'
    )
    pdf.write_body(
        'WxBridge 用反射调用微信 SDK，编辑器里找不到类型时直接进入游戏，不阻塞启动。'
        '真机上初始化 SDK、覆盖触控，并读取右上角胶囊按钮下沿，避免顶栏数值被系统按钮挡住。'
        'GM 调试条只在编辑器或开发者工具模拟器出现，真机不显示。'
    )
    pdf.write_code_block([
        'Boot():',
        '  targetFrameRate = 60',
        '  if no GameFlow: create InkLine and add GameFlow',
        '',
        'GameFlow.Start(): WxBridge.InitSdk -> CloudSync.Startup(Begin)',
        'Begin(): load MetaProgress, create canvas, ShowHome()',
    ])

    pdf.write_h2('3.2 战斗世界与炮串移动')
    pdf.write_body(
        'BattleWorld.Begin 接收关卡、锻造结果和已装备道具。它清掉上一局的敌人、子弹和宝箱，'
        '按关卡格子掩码打开字格，把开局金币、炮数、伤害系数、射速系数、暴击率和基地生命写成这一局的初始值，'
        '再把皮肤的固定加伤和开局金币补上去。局外等级不会在战斗中途再改这些系数。'
        '装着赤焰时暴击词条生效，装着福袋时局内金币收益生效，换下皮肤后这两条不参与本局。'
    )
    pdf.write_table(
        ['开局量', '来源', '说明'],
        [
            ['金币', '锻造开局金币 + 皮肤加金', '默认 6，锻造每级 +8，黄金炮另 +20'],
            ['炮数', '锻造炮台数', '默认 2，局外最多 4；局内看广告可加到 6'],
            ['伤害系数', '锻造伤害线', '每级 +16%，再加皮肤固定伤害'],
            ['射速系数', '锻造射速线', '每级开火间隔 -8%'],
            ['暴击', '赤焰专属词条', '装着才生效，暴击伤害为两倍'],
            ['基地生命', '锻造生命，孤城规则除外', '默认 3；孤城关强制为 1'],
        ],
        [28, 52, 85],
    )
    pdf.write_body(
        '炮串只在漏怪线以下的区域响应拖动。按住时炮串跟随手指的世界坐标 x，松手后吸附到最近的列中心。'
        '每门炮按自己的冷却发射。子弹出生在对应列，向上飞行，穿过该列每一行已放置且已生效的字牌，把改动收进 ShotMods。'
        '战斗中可以看一次激励视频多加一门炮，加到六门为止。这扇门只存在于本局，不写进锻造等级。'
    )
    pdf.write_code_block([
        'HandleRail():',
        '  if pointer down in rail zone: dragging = true',
        '  if dragging: world.SetRailFromWorldX(pointer.x, snapOnRelease)',
        '',
        'TickEmitters(dt):',
        '  for each emitter: cooldown -= dt',
        '  if cooldown <= 0: spawn bullet on that column',
    ])

    pdf.write_h2('3.3 字牌、词组、招牌与命中结算')
    pdf.write_body(
        '字牌分三类。常驻字过格就改弹，例如火、冰、分、瞄、速，以及后续章节的金、木、水、土、风、雷、毒、惑。'
        '蓄能字每过若干发才触发一次，例如穿、炸、重、晕。词组字单放不生效，同列凑齐配对才醒：秒与杀、万与箭、击与退、连与斩。'
        '商店抽出的牌永远是 1 星。同一格叠到 2 星、3 星后，数值按字表放大。'
    )
    pdf.write_table(
        ['类型', '代表字', '生效条件'],
        [
            ['常驻', '火 冰 分 瞄 速 金 木 水 土 风 雷 毒 惑', '子弹每穿过一次就改 ShotMods'],
            ['蓄能', '穿 炸 重 晕', '同列累计过弹，蓄满的那发才触发'],
            ['词组', '秒杀 万箭 击退 连斩', '同列两字都在，且该发蓄满词组充能'],
            ['招牌', '霜火 焦雷 霰雷 燎毒 导电 熔金 等十二对', '同一发上两种字都有星，命中前改一次数值'],
        ],
        [22, 78, 65],
    )
    pdf.write_body(
        '所有伤害、状态和位移都进 ResolveHit，避免每个字各写一套扣血。顺序是：先破盾；'
        '再算基础伤害乘以分裂或穿透带来的衰减；加算池与乘算池按规则合并，默认加算不吃乘算；'
        '然后扣护甲，斩杀只会把伤害抬高；接着吸血、扣血、上状态、做位移。范围伤害不再套一层斩杀，避免爆炸里再爆炸。'
    )
    pdf.write_code_block([
        'ResolveHit(enemy, mods, source):',
        '  if shield: break shield; if not execute: return',
        '  dmg = base * decay',
        '  dmg = addThenMul(dmg, mods)',
        '  dmg = applyArmor(dmg, enemy.Armor)',
        '  if execute: dmg = raiseToExecuteLine(dmg, enemy)',
        '  applyLeech, subtractHp, applyStatus, applyMove',
    ])
    pdf.write_body(
        '硬控同时只保留一个，优先级为冻高于晕、晕高于惑，没抢到的降成等时长的减速。'
        '毒可以叠层。域效果，例如爆炸和雷圈，使用同一套伤害公式，但不再触发新的域。'
        '招牌在第一次命中前只改已经收集好的数值，例如让灼烧提高、让加算也吃乘算，之后仍走上面的骨架。'
        '三字、四字组合不另写结算，仍按五槽叠色和普通命中处理。'
    )

    pdf.write_h2('3.4 出怪、敌人与关卡规则')
    pdf.write_body(
        '关卡共八章，章名为草地、土路、石院、沙漠、森林、河滩、雪原、火山。每章九关，最后一关是章底。'
        'StageCatalog 在首次读取时生成全部关卡：每关有开放格子、牌池、波次表、规则标记，以及按波次加总的赏金和墨统计。'
        '格子从中间列向两侧开。前期出怪更少，并偏向已经开了格子的列。'
    )
    pdf.write_body(
        '敌人从画面上方按波次时间点出生，默认向下走。漏过底线扣基地生命。生命归零失败；波次清空且没有存活敌人则胜利。'
        '普通敌人用血量、速度、赏金、掉墨和半径区分。部分敌人带盾、横移、护甲、半血加速、免控、免击退、治疗光环或死亡分裂。'
        '八个章底头目单独配置，不与普通兵共用一只再放大血量。图鉴按关卡里第一次出现的顺序收录。'
    )
    pdf.write_table(
        ['关卡规则', '效果'],
        [
            ['残卷', '开局若干格已经摆好字'],
            ['限字', '牌池只保留指定的几个字'],
            ['疾行', '敌人更快，金币也更多'],
            ['丰年', '刷怪更多，金币更多'],
            ['孤城', '基地只有 1 血，首通墨更多'],
            ['残局', '用掩码逐格开关，留下窄路'],
            ['禁道具', '本关不带道具，首通墨更多'],
        ],
        [36, 129],
    )
    pdf.write_body(
        '血量系数按章递增，章内每一关再略增，章底再乘一次。第三章以后格子已经铺满，曲线比前三章更平。'
        '抽牌价按这一关的金币收入反推，避免后期金币变多后改装变得没有取舍。新字按章节放进牌池，不在局外用墨购买。'
        '每只怪掉多少金币和墨写在敌人表上，关卡上的墨统计只是把波次加总，不再反过来决定单只掉落。'
    )

    pdf.write_h2('3.5 改装抽牌与放置升星')
    pdf.write_body(
        '改装不跟波次绑定。本局金币大于等于当前抽牌价时，战斗不会立刻打断正在滑动的炮串；'
        '松手并稳定约 0.35 秒后自动扣费、暂停并打开抽屉。玩家也可以点改装键提前打开。'
        '关掉抽屉后有一段静默，避免刚放完字又被下一张牌打断。'
    )
    pdf.write_body(
        '抽牌从本关牌池里不放回地抽最多三张。池子不足三张时少发，避免第一关只剩一个字却抽出三张相同牌。'
        '词组字如果场上已经有它的配对，权重提高；没有配对时权重降低。'
        '玩家点一张牌后进入放置。点空格放下 1 星；点同名且未满星的格子升一星；点已有不同字的格子要先确认覆盖。'
        '满星格子拒绝放置。没开放的格子点了会提示尚未开启。取消抽牌会把刚扣的金币退回，并撤销这一次抽牌计数。'
        '每关可看一次激励视频重刷当前牌。格子每次放上、升星或覆盖都会给短反馈和音效。前两关在教学点上会给出滑动和升星提示。'
    )
    pdf.write_code_block([
        'OpenDraft():',
        '  if gold < cost: return',
        '  gold -= cost; draftCount += 1; paused = true',
        '  offer = weightedSample(stage.pool, up to 3)',
        '',
        'Place(card, col, row):',
        '  if cell locked or already 3 star: reject',
        '  if same card: star += 1',
        '  else if occupied: need confirm, then overwrite',
        '  else: put 1 star',
        '  refresh column charges and words',
    ])
    pdf.write_body(
        '抽牌价从关卡的首次价格起，每次成功抽牌按这一关算好的步长增加。'
        '因此玩家要在「现在抽一张」和「把炮留在当前列继续打」之间取舍，而不是打完一波自动获得强化。'
    )
    p, c = img('draft')
    pdf.write_image(p, c)
    p, c = img('place')
    pdf.write_image(p, c)

    pdf.write_h2('3.6 掉落、墨摊与战场宝箱')
    pdf.write_body(
        '击杀掉落分成金币、墨和战场宝箱，都只在本局生效。通关时才把拾到的墨写入存档。'
        '金币按敌人自己的赏金拆成多枚，从落点弹起再飞向顶栏。墨按敌人自己的掉墨量计算，零头攒够 1 才出一摊；'
        '杂兵可以不掉墨。死亡后先在原地摊开，再收成一颗墨珠飞入顶栏。'
        '场上同时飞行的掉落物有上限，超出的直接记账，避免清屏时糊住战场。'
        '失败或中途撤退时，本局拾到但尚未通关入账的墨不写入 MetaProgress。'
    )
    pdf.write_body(
        '战场宝箱不是敌人：不走路、不打基地、不计入波次胜负，只吃炮弹。击杀时按关卡概率掷一次，场上同时最多两个。'
        '箱子分金币箱和墨箱。存在一段时间，打破才把内容计入本局金币或本局墨，超时直接消失。'
        '打箱子使用和命中敌人相同的基础伤害、衰减、加算和乘算，但不吃斩杀和状态。'
        '这和通关后放到首页槽位里的木箱、银箱、金箱、皇家箱不是同一种东西。'
    )
    pdf.write_code_block([
        'DropLoot(enemy):',
        '  scatter gold by enemy.Gold',
        '  carry += enemy.Ink; spill whole ink as one puddle',
        '  RollChest(enemy)',
        '',
        'DamageChest(chest, dmg):',
        '  if hp <= 0: grant amount to run gold or run ink',
    ])
    p, c = img('chest')
    pdf.write_image(p, c)

    pdf.write_h2('3.7 局内自动道具')
    pdf.write_body(
        '道具与字牌分开：不进牌池、不占格子、不吃星，也不花本局金币或局外墨。'
        '首页最多装备三个。第一格开局就有，通关第三章开第二格，通关第六章开第三格。'
        '带禁道具规则的关卡三个都不生效。开局先走半段冷却，避免第一秒场上没怪就空放。'
    )
    pdf.write_body(
        '冷却从图标底部灌满。灌满后进入就绪，等该道具自己的场面条件满足才丢出，条件不对就继续等，不白放。'
        '丢出后先播放飞向目标的演出，落地那一刻才改血量或状态，避免特效和结算错位。'
        '道具不走 ResolveHit 的斩杀链。伤害类道具按当前基础弹伤的倍数计算，这样后期关卡不会被固定点数打不动。'
        '能量饮料是一段炮弹增伤，不叠进道具自己的伤害倍数里。急救包每局次数有限，满级可以放两次。'
    )
    pdf.write_table(
        ['道具', '品质', '自动条件', '效果概要'],
        [
            ['鞭炮', '普通', '最前排有 2 个敌人', '最前排炸开一圈'],
            ['胶水', '普通', '有敌人过了半场', '全场减速一阵'],
            ['弹弓', '普通', '场上有敌人', '最前一个吃一记重击'],
            ['闹钟', '高级', '敌人逼近或场上较多', '全场定身'],
            ['能量饮料', '高级', '场上较多或首领在场', '短时间炮弹伤害提高'],
            ['冰块', '高级', '场上达到一定数量', '全场冰伤并减速'],
            ['大扫把', '稀有', '场上很多或敌人逼近', '全屏伤害并击退'],
            ['辣椒酱', '稀有', '同一列有 3 个敌人', '那一列灼烧数秒'],
            ['急救包', '稀有', '基地掉血后', '基地回血，每局次数有限'],
        ],
        [28, 18, 52, 67],
    )
    pdf.write_body(
        '品质决定升级要的道具卡数量和折成墨的单价，不改变「冷却好了才自动放」这条规则。'
        '等级缩短冷却并加强效果。满级为 5。'
    )
    p, c = img('item_cast')
    pdf.write_image(p, c)

    pdf.write_h2('3.8 首页、出征、体力与钻石')
    pdf.write_body(
        '首页由 HomeScreen 构建。有预制体时绑定预制体上的按钮和文本，没有时用代码搭出同样的三页结构。'
        '体力上限 12。普通关消耗 2 点，章底消耗 3 点，每 15 分钟恢复 1 点。'
        '满体力时不每帧写盘，只在消耗或恢复发生时保存。把系统时间往回拨不会白拿体力。'
        '每日重置广告补体次数、钻石补体次数和每日首胜标记。'
    )
    pdf.write_body(
        '出征页展示当前章的关卡。前一关未通关时后一关锁定。点击继续时检查体力是否够该关消耗，够则扣除并进入战斗。'
        '进关即扣，失败、撤退、中途回首页都不退。撤退前先暂停，并写明当前完成度和「没有奖励、体力不退」。'
        '体力不足时可以看激励视频补充，每日次数有限；也可以花 10 钻石换 10 体力，每天最多 3 次，且体力未满才能换。'
        '首次进入会发一笔初始墨并把体力补满。'
    )
    pdf.write_code_block([
        'StartStage(index):',
        '  if stamina < stage.StaminaCost: return',
        '  SpendStamina(cost)',
        '  world.Begin(stage, meta.Forged, meta.Equipped)',
        '  world.ApplySkin(meta.Skin)',
        '  show battle start, then battle hud',
        '',
        'AskRetreat(): pause, show progress, confirm or stay',
    ])

    pdf.write_h2('3.9 炮台成长与皮肤')
    pdf.write_body(
        '炮台页上半部是轮播展台，下半部是成长词条。通用词条只改开局条件，不解锁、不强化任何字牌。'
        '每条线有价格和通关关数门槛，门槛铺到后期章节。已经买过的线一直显示；还没到的线只预告下一张。'
        '专属词条只有拥有对应皮肤才露面，只有装着那款皮肤才生效。'
        'ForgeCatalog.Stats 把等级和当前皮肤折成 ForgeStats，BattleWorld.Begin 只认这个结构。'
    )
    pdf.write_table(
        ['加成', '幅度', '说明'],
        [
            ['伤害', '每级 +16%', '乘在整发炮弹上，门槛铺到后期'],
            ['炮台数', '每级多 1 门', '局外最多 4 门'],
            ['射速', '每级间隔 -8%', '不在局内再叠'],
            ['开局金币', '每级 +8', '用来先手改装'],
            ['基地生命', '每级 +1', '孤城关仍只有 1 血'],
            ['暴击', '赤焰专属，每级 +4%', '装着赤焰才生效'],
            ['金币收益', '福袋专属，每级 +6%', '只加局内金币，不加墨'],
        ],
        [32, 48, 85],
    )
    pdf.write_body(
        '六款炮台是小钢炮、糖果炮、机甲炮、黄金炮、赤焰、福袋。小钢炮一开始就有。'
        '糖果炮看一次广告获得。机甲炮在第一轮七日签到的第 7 天获得，并给每发炮弹一点固定伤害。'
        '黄金炮通关第二章后花墨购买，开局多 20 金币。赤焰和福袋标记为活动获取，活动未开启时不能领取，'
        '分别对应暴击和金币收益两条专属词条。素色和广告炮只换外观，不改字牌的加算池和乘算池规则。'
    )
    p, c = img('forge')
    pdf.write_image(p, c)

    pdf.write_h2('3.10 道具成长与装备')
    pdf.write_body(
        '九个道具都从 0 级开始。解锁只消耗道具卡，升级才再花墨。卡主要从通关宝箱、新手礼包和签到宝箱开出。'
        '普通、高级、稀有要的卡数不同，稀有更少。升到下一级后按卡数进度重新累计。满级为 5。'
        '满级之后再开到的同名卡折成墨。0 级升到 1 级时，若还有空着的已开启槽位，会自动装上。'
    )
    pdf.write_body(
        '装备在道具页完成。上面是三个槽，下面是九张道具卡。点已装备的可以卸下，点未装备的填第一个空槽。'
        '不能装备未解锁的道具，也不能把同一个道具放进两个槽，也不能往还没通关解锁的槽里装。'
    )
    pdf.write_code_block([
        'BuyItem(i):',
        '  if cards < nextCards: return',
        '  if level > 0 and ink < nextPrice: return',
        '  spend cards and ink; level += 1',
        '  if was locked and a slot is open: Equip(i)',
    ])
    p, c = img('item')
    pdf.write_image(p, c)

    pdf.write_h2('3.11 结算、评星与通关宝箱')
    pdf.write_body(
        '胜利只结算一次。评星看剩余基地生命：大约七成以上为三星，三成五以上为两星，否则一星。'
        '本关用过续命则最高两星，三星必须自己守下来。星级只保留历史最高，重复通关不会把星打回去。'
    )
    pdf.write_body(
        'ApplyResult 把本局拾到的墨加入存档。当天第一次胜利把这笔墨再乘二，不再另回复体力。'
        '首次通关按关卡类型给钻石：普通关、有首领的关、章底数量不同。重复通关不给钻石。'
        '同时按轮换表发放通关宝箱。普通关在木、银、金之间轮换；有首领的关给金箱；章底首通给皇家箱，再打给金箱。'
        '宝箱先挂在结算页上。看激励视频可以同时把刚入账的墨再加一倍，并当场开掉这只宝箱。'
        '不看视频就离开时，宝箱放进出征页的四个槽位；槽位已满则按该档平均墨折算入账，不占位。'
        '这个入口只出现一次，并且不会再次调用 ApplyResult。'
    )
    pdf.write_body(
        '失败不给墨、钻石和宝箱。若本关续命次数未到上限，先进入续命页，成功后恢复战斗而不是重开整关。'
        '每关最多续命两次。不续、没得续、或者续了又失败，才进入失败页。失败页按完成度给出说明。'
        '通关页、续命页、失败页是三套面板，不共用一个结果框。'
    )
    p, c = img('victory')
    pdf.write_image(p, c)
    p, c = img('revive')
    pdf.write_image(p, c)
    p, c = img('defeat')
    pdf.write_image(p, c)
    pdf.write_body(
        '放进槽位的宝箱按档位等待：木箱、银箱较短，金箱、皇家箱更长。等待结束可以打开，'
        '也可以按剩余时间花钻石立即打开，大约每 30 秒折 1 钻。'
        '开箱只算出墨和若干叠道具卡，再由 MetaProgress 入账。卡优先给没满级的道具，装着的道具权重大一些；'
        '皇家箱的稀有卡优先给还没解锁的道具。某一品质全都满级时，那一叠改成墨。'
    )
    pdf.write_code_block([
        'ApplyResult(stage, collected, stars):',
        '  if first clear: diamonds by stage kind',
        '  pending chest = tier for this stage',
        '  if new best: save stars',
        '  inkGain = collected; if first win today: inkGain *= 2',
        '  Ink += inkGain; Save()',
        '',
        'OpenChest(slot): if timer done or diamonds paid: Roll and ApplyLoot',
    ])
    p, c = img('chest_open')
    pdf.write_image(p, c)

    pdf.write_h2('3.12 签到、礼包、游戏圈与图鉴')
    pdf.write_body(
        '七日签到断一天就从第 1 天重来。每天给墨、钻石和体力，数量按当天的表。'
        '第 7 天另给一个金宝箱；第一轮签满且还没有机甲炮时，送出机甲炮。'
        '当天可以看广告把这一天的墨、钻石和体力再领一份。'
    )
    p, c = img('checkin')
    pdf.write_image(p, c)
    pdf.write_body(
        '新手礼包需要看两次激励视频，领取一次后消失。奖励是当场打开的银宝箱、30 钻石和 100 墨，不占宝箱槽。'
    )
    p, c = img('gift')
    pdf.write_image(p, c)
    pdf.write_body(
        '游戏圈每天发一条帖子可以领一次，奖励是墨加一个木宝箱。发帖数由云函数解密微信游戏圈数据后返回，客户端不自己算。'
    )
    p, c = img('club')
    pdf.write_image(p, c)
    pdf.write_body(
        '图鉴分三栏。字谱收单字和词组，秘卷收十二对招牌，墨谱收敌人。'
        '字和招牌要在关卡里实际出现过才点亮；敌人打倒过才写入墨谱，只见过的只露轮廓。'
        '每一栏按大约三分之一、三分之二和收齐设三档墨奖励，领取后写入存档，不重复发。'
    )
    p, c = img('codex')
    pdf.write_image(p, c)

    pdf.write_h2('3.13 排行榜与云存档')
    pdf.write_body(
        '排行榜按已通关关数排序，相同关数时更早到达的在前。客户端在通关后上报当前通关数、昵称和头像。'
        '同样的关数和资料只报一次。服务端只接受更高的通关数，本地清档或换设备不会把榜上成绩拉下来。'
        '打开榜单时先补报还没送上去的成绩，再拉取前若干名和自己的名次。'
        '编辑器里的调试全通关使用匿名身份，不展示给真机玩家。'
    )
    p, c = img('rank')
    pdf.write_image(p, c)
    pdf.write_body(
        '云端存档只有一个键，值是 MetaProgress 的 JSON。本地每次保存都会把同步标记为脏，并在短防抖后上行。'
        '切到后台或清档时立即上行。请求带上本地更新时间和上次见到的云端时间。'
        '若云端已经更新，服务端拒绝旧档回写，客户端改为下行覆盖。'
        '令牌过期时清掉并重新登录一次。微信环境用登录码换身份，其它环境用本机匿名标识。'
    )
    pdf.write_code_block([
        'Save(): write inkline.meta.v2; CloudSync.Touch()',
        'Push: updatedAt + baseRemoteUpdatedAt + payload.meta',
        '409 STALE_UPDATE: pull remote and replace local',
        'Rank submit: cleared only increases',
    ])

    pdf.write_h1('四、数据结构设计')
    pdf.write_h2('4.1 局外存档')
    pdf.write_code_block([
        'MetaProgress = {',
        '  Ink, Diamond, Stamina, StaminaTick,',
        '  AdStaminaToday, DiamondStamCount, LastDay, DailyWinDone,',
        '  Stars[72],          // 0 未通关, 1~3 为历史最高星',
        '  Forge[],            // 通用词条和专属词条的等级',
        '  Skin, SkinOwned[6],',
        '  ItemLevel[9], ItemCards[9], Equipped[3],',
        '  ChestSlot[4], ChestCycle, PendingChest,',
        '  GiftAds, GiftClaimed, ClubDay,',
        '  CheckDay, CheckRun, CheckLoops, CheckSkinDone,',
        '  CodexWord, CodexPair, CodexEnemy, CodexMileClaim',
        '}',
    ])

    pdf.write_h2('4.2 一局战斗状态')
    pdf.write_body(
        '一局状态不写入存档。退出战斗或完成本局后只保留结算进 MetaProgress 的那部分。'
        '字格、星级、列充能、金币、本局墨、敌人、子弹、战场宝箱和道具冷却都随着 BattleWorld 释放而消失。'
    )
    pdf.write_code_block([
        'RunState = {',
        '  Stage, Open[col,row], Grid[col,row], Stars[col,row],',
        '  Gold, Ink, EmitterCount, RailX, BaseHp, RevivesUsed,',
        '  Enemies, Bullets, Chests, Drops,',
        '  ItemSlots[3], ItemCooldown[3], DraftCount, Paused',
        '}',
    ])

    pdf.write_h2('4.3 字牌与子弹改动')
    pdf.write_code_block([
        'CardDef = { Id, Name, Blurb, Wake, Word, ChargeNeed }',
        'ShotMods = {',
        '  BaseDamage, AddDamage, MulDamage, Decay,',
        '  Leech, Pierce, ExplodeR, StatusHits[],',
        '  PushColumn, PushLateral, InstantKill, Word, Tuned',
        '}',
        'SignaturePair = { A, B, Name, Form, Note }',
    ])
    pdf.write_body(
        'BulletActor 只保存位置、速度和已经击中过的目标编号。具体造成什么效果全部读 ShotMods。'
        '新增一个字，只需要在字表里声明它属于哪一族，以及它写入 ShotMods 的哪个字段。'
        '新增一对招牌，只在命中前调整已经写好的数值，不增加结算步骤。'
    )

    pdf.write_h2('4.4 关卡、敌人、道具与宝箱')
    pdf.write_code_block([
        'StageDef = {',
        '  Index, Chapter, Title, OpenCells or Mask,',
        '  Pool, Waves, Bodies, Rules, Preset,',
        '  Hp, StaminaCost, KillGold, InkBudget, DraftFirst, DraftStep',
        '}',
        'EnemyDef = { Id, Hp, Speed, Gold, Ink, Radius, traits }',
        'ItemDef = { Id, Name, Quality, Cooldown, When }',
        'ChestDef = { Tier, Seconds, InkMin, InkMax, Cards }',
        'ForgeStats = { DamageMul, IntervalMul, Emitters, StartGold, BaseHp, CritChance, GoldMul }',
    ])

    pdf.write_h1('五、数据接口设计')
    pdf.write_h2('5.1 流程接口')
    pdf.write_body('界面与流程之间只通过少量入口通信，避免面板直接改模拟对象。')
    pdf.write_code_block([
        'GameFlow.StartStage(index)',
        'GameFlow.OpenDraft() / CloseDraft()',
        'BattleWorld.Place(card, col, row, overwrite)',
        'BattleWorld.Tick()           // 道具在此自动释放',
        'BattleWorld.TryRevive()',
        'MetaProgress.ApplyResult(stage, collectedInk, stars)',
        'MetaProgress.OpenChest(slot) / OpenPending()',
    ])

    pdf.write_h2('5.2 配置读取接口')
    pdf.write_table(
        ['接口', '作用'],
        [
            ['StageCatalog.Get(index)', '取一关的波次、牌池、规则和经济'],
            ['CardCatalog.Get(id)', '取字的名称、唤醒方式和配对'],
            ['ItemCatalog.Blurb(def, level)', '按等级和当前弹伤生成道具说明'],
            ['ChestCatalog.ForStage(...)', '决定这一关发哪一档通关宝箱'],
            ['ForgeCatalog.Stats(levels, skin)', '把锻造等级和皮肤折成开局数值'],
            ['CodexCatalog.Miles(tab)', '取图鉴三档收集奖励'],
        ],
        [62, 103],
    )

    pdf.write_h2('5.3 本地存储与云端接口')
    pdf.write_body(
        '本地只使用 PlayerPrefs 的字符串读写。云端不保存战斗中的棋盘，只保存局外 JSON。'
        '微信小游戏环境下，引擎会把 PlayerPrefs 落到平台本地存储。'
        '云函数路由都接收 JSON，登录之后的请求带 Bearer 令牌。'
    )
    pdf.write_table(
        ['接口', '作用'],
        [
            ['POST /login', '用微信登录码或匿名标识换 JWT'],
            ['POST /save/pull', '拉取当前用户存档'],
            ['POST /save/push', '按更新时间合并上传，拒绝旧档覆盖'],
            ['POST /rank/submit', '上报已通关关数，只升不降'],
            ['POST /rank/list', '取通关榜前若干名和自己的名次'],
            ['POST /gameclub/daily', '解密当日游戏圈发帖数'],
            ['GET /health', '健康检查'],
        ],
        [52, 113],
    )

    pdf.write_h2('5.4 平台接口')
    pdf.write_body(
        'WxBridge 不把微信类型写进业务程序集。它按类型名反射查找初始化、系统信息和触控覆盖组件。'
        '找不到时所有调用变成空操作，编辑器播放和正式小游戏共用 GameFlow。'
        '激励视频由 AdStub 接收位置名，回调成功后才改对应数据。位置包括重刷改装、续命、结算翻倍开箱、补充体力、加炮、签到翻倍、新手礼包和获取糖果炮。'
    )

    pdf.write_h1('六、出错处理设计')
    pdf.write_h2('6.1 存档损坏与版本迁移')
    pdf.write_body(
        'JSON 解析失败时丢弃该字符串并使用新档，不让异常中断启动。字段缺失由序列化默认值补上。'
        '数组短于当前版本时拷贝已有项并补齐：通关星、锻造、皮肤、道具等级、道具卡、装备槽和宝箱槽都这样处理。'
        '锻造等级夹到该线的最大级。装备槽若指向未解锁道具、未开启的槽，或两个槽重复，会被改成空槽。'
        '更早的旧档在读取时折进新结构，再写入当前键，避免玩家进度整档作废。'
    )

    pdf.write_h2('6.2 操作被拒绝')
    pdf.write_body(
        '体力不足、金币不足、格子未开、字已满星、道具未学会、槽位未解锁、禁道具关，都在入口直接返回，并给出短提示。'
        '覆盖异类字必须经过确认面板，取消则保持放置态，不丢弃已经抽到的那张牌。'
        '取消整个三选一会退回本次金币和抽牌次数。'
        '撤退必须确认。钻石或广告次数用完时，对应按钮不可用，不扣资源。'
    )

    pdf.write_h2('6.3 重复结算与云端冲突')
    pdf.write_body(
        '进入结算会先把屏幕状态改成结果，并暂停世界。翻倍和当场开箱只追加墨或消耗挂起的那一只宝箱，不再调用 ApplyResult。'
        '续命成功才把屏幕改回战斗。胜利和失败不会在同一局里既入账又退回。'
        '云端返回旧档冲突时，不重试覆盖，改为下载云端档。只读缓存状态下不上传。'
        '排行榜重复上报同一成绩时直接跳过。服务端拒绝降分。'
    )

    pdf.write_h2('6.4 平台与资源缺失')
    pdf.write_body(
        '微信 SDK、胶囊区域、触控覆盖或云函数不可用时，游戏仍能完成启动、战斗和本地存档。'
        'CDN 图片失败时保留缩略图，并在一段时间后才允许重试。问不到运行平台时按真机处理，不显示调试入口。'
        '时间回拨只修正体力锚点，不增加体力。'
    )

    pdf.write_h1('七、性能优化设计')
    pdf.write_h2('7.1 模拟与绘制分离')
    pdf.write_body(
        'BattleWorld.Tick 只改数值和列表。BattleView 在同一帧之后按编号同步精灵，死亡和过期对象从列表移除，'
        '避免界面逻辑参与碰撞。飘字、墨珠、道具演出和爆点有独立的短生命周期，到时销毁。'
        '道具伤害等到演出落地再结算，不在抛出的那一帧做全屏查询以外的重复扣血。'
    )

    pdf.write_h2('7.2 配置一次生成')
    pdf.write_body(
        '七十二关在 StageCatalog 第一次被访问时生成并缓存。每关的击杀赏金、墨统计、出怪数量和抽牌价在生成时算完，'
        '战斗中只读，不再每帧扫描波次表。字表、道具表、宝箱表和锻造表是静态数据。'
    )

    pdf.write_h2('7.3 存档与网络')
    pdf.write_body(
        '大厅每帧会检查日期和体力恢复，但只有真正跨天或恢复出点数才写盘。满体力时只更新内存中的时间锚点。'
        '锻造、买皮肤、升级道具、开箱和结算各写一次，不在拖动炮串时写档。'
        '云上行有防抖，失败后退避并限制连续失败次数，避免每帧重试。CDN 同时下载数有上限。'
    )

    pdf.write_h2('7.4 表现复用')
    pdf.write_body(
        '字效按槽叠加，同槽只保留优先级最高的外形，不同槽可以同时出现。'
        '掉落物超过上限后改为直接记账。界面在预制体缺失时用代码搭建同一套控件，保证没有美术资源时逻辑仍可运行。'
        '高清章节图不全部常驻，超过上限后淘汰当前没在显示的纹理。'
    )

    pdf.write_h1('八、结论')
    pdf.write_body(
        f'{SOFTWARE_FULL_NAME} {SOFTWARE_VERSION}围绕列式字牌改装、持续塔防战斗、自动道具、通关宝箱，'
        '以及本地存档与云端合并建立了完整结构。'
        '战斗公式集中在一次命中结算，关卡、字牌、敌人、道具和成长由配置表驱动，界面与平台能力不反向修改规则。'
        '上述设计满足软件著作权登记文档鉴别材料对技术说明的要求。'
    )


def validate_pdf():
    from pypdf import PdfReader
    reader = PdfReader(str(OUTPUT))
    return len(reader.pages)


def main():
    pdf = DocPDF()
    pdf.add_font('Songti', '', SONGTI_PATH)
    write_document(pdf)
    pdf.output(str(OUTPUT))
    pages = validate_pdf()

    print('=' * 60)
    print('  墨字防线软著文档鉴别材料 PDF 生成报告')
    print('=' * 60)
    print(f'  软件名称:     {SOFTWARE_FULL_NAME} {SOFTWARE_VERSION}')
    print(f'  申请人:       {APPLICANT_NAME}')
    print(f'  项目路径:     {PROJECT_ROOT}')
    print(f'  文档类型:     设计说明书')
    print(f'  生成页数:     {pages} 页')
    print(f'  输出文件:     {OUTPUT}')
    if pdf.missing_images:
        print('  缺少截图:     以下图片已在 PDF 中使用占位框')
        for path, caption in pdf.missing_images:
            print(f'    - {Path(path).name}: {caption}')
    else:
        print('  截图检查:     已找到全部截图')
    print('=' * 60)


if __name__ == '__main__':
    main()
