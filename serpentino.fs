#! /usr/bin/env gforth

\ Serpentino

: version s" 0.30.0+201711270118" ;
\ See change log at the end of the file.

\ Description:
\
\ A text-based snake game under development
\ written in Forth (http://forth-standard.org)
\ for Gforth (http://gnu.org/software/gforth).

\ Author: Marcos Cruz (programandala.net)
\ http://programandala.net
\ http://github.com/programandala-net/serpentino

\ =============================================================
\ License

\ You may do whatever you want with this work, so long as you
\ retain all copyright, credit and authorship notices, and this
\ license.  There is no warranty.

\ =============================================================
\ Credit

\ Forked on 2017-11-22 from the initial commit of Robert
\ Pfeiffer's forthsnake
\ (https://github.com/robertpfeiffer/forthsnake), 2009.

\ ==============================================================
\ Requirements

\ From Gforth

require colorize.fs

\ From Galope

require galope/between.fs                  \ `between`
require galope/e-key-to.fs                 \ `ekey>`
require galope/minus-keys.fs               \ `-keys`
require galope/question-one-minus-store.fs \ `?1-!`
require galope/random-between.fs           \ `random-between`
require galope/unhome.fs                   \ `unhome`

\ ==============================================================

variable colorize colorize on
  \ Flag.

<a black >bg red   >fg a> value apple-attr
<a black >bg           a> value arena-attr
<a black >bg red   >fg a> value crush-attr
<a black >bg green >fg a> value snake-attr
<a black >bg           a> value status-attr
<a black >bg green >fg a> value text-attr
<a black >bg white >fg a> value wall-attr
  \ Color attributes.

: ?attr! ( x -- ) colorize @ if attr! else drop then ;
  \ If `colorize` is non-zero, set color attribute _x_.

variable score
variable record record off
  \ Counters.

variable delay
  \ Crawl delay in ms.

192 constant initial-delay
  \ Initial crawl delay in ms.

4 constant acceleration
  \ Delay decrement.

cols 2 - constant arena-cols
rows 4 - constant arena-rows
  \ Size of the arena.

cols arena-cols - 2/ constant arena-x
rows arena-rows - 2/ constant arena-y
  \ Coordinates of the top-left corner of the arena.

arena-x arena-cols + 1- constant arena-max-x
arena-y arena-rows + 1- constant arena-max-y
  \ Coordinates of the bottom-right corner of the arena.

arena-x 1- constant wall-x
arena-y 1- constant wall-y
  \ Coordinates of the top-left corner of the wall.

arena-max-x 1+ constant wall-max-x
arena-max-y 1+ constant wall-max-y
  \ Coordinates of the bottom-right corner of the wall.

wall-max-x wall-x - 1+ constant wall-cols
wall-max-y wall-y - 1+ constant wall-rows
  \ Size of the wall.

4 constant initial-length
  \ Initial length of the snake.

512 constant max-max-length
  \ Limit of the calculated maximum length of the snake, no
  \ matter the size of the screen. This limit makes sure the
  \ buffer is big enough, no matter if the size of the screen
  \ is increased.

: (max-length) ( -- n )
  arena-rows arena-cols * 5 / max-max-length min ;
  \ Return maximum length _n_ of the snake, calculated after
  \ the size of the arena.

(max-length) value max-length
  \ Maximum length of the snake.

2 cells constant /segment
  \ Size of each snake's segment.

create snake  max-max-length /segment * allot
  \ Snake's segments. Each segment contains its coordinates.

2variable apple
  \ Coordinates of the apple.

variable head>
  \ Number of the head segment.

2variable previous-head
  \ Coordinates of the head before crawling.

variable length
  \ Snake's current length.

2variable direction
  \ Snake's current direction (coordinate increments).

: segment ( n -- a )
  head> @ + max-length mod /segment * snake + ;
  \ Convert segment number _n_ to its address _a_.

: head ( -- a ) 0 segment ;
  \ Return address _a_ of the snake's head segment.

: neck ( -- a ) 1 segment ;
  \ Return address _a_ of the snake's "neck" segment, ie.
  \ its second segment.

: tail ( -- a ) length @ segment ;
  \ Return address _a_ of the snake's tail segment.

: clash? ( a1 a2 -- f ) 2@ rot 2@ d= ;
  \ Are the coordinates contained in _a1_ equal to the
  \ coordinates contained in _a2_?

: cross? ( a -- f ) head clash? ;
  \ Does the head cross segment _a_?

: random-xy ( -- col row ) arena-x arena-max-x random-between
                           arena-y arena-max-y random-between ;

: segment? ( col row -- f )
  length @ 0 ?do     2dup  i segment 2@ d=
                  if 2drop true  unloop exit then
             loop    2drop false ;
  \ Is there a snake's segment at _col row_?

: apple-random-xy ( -- col row )
  begin random-xy 2dup segment? while 2drop repeat ;

: new-apple ( -- ) apple-random-xy apple 2! ;
  \ Locate a new apple.

wall-x             constant status-x
wall-max-y 1+      constant status-y
wall-max-x         constant status-max-x
  \ Coordinates of the status bar.

status-max-x status-x - 1+ constant status-cols
  \ Columns of the status bar.

: score$ ( -- ca len ) s" Score: " ;
  \ Return the score label _ca len_.

status-x 1+ constant score-label-x
status-y    constant score-label-y
  \ Coordinates of the score label.

: .score$ ( -- )
  text-attr ?attr!
  score-label-x score-label-y at-xy score$ type ;
  \ Display the score label.

4 constant digits

: .(score) ( n -- )
  text-attr ?attr! s>d <# digits 0 ?do # loop #> type ;

: score-xy ( -- col row )
  score-label-x score$ nip + score-label-y ;
  \ Return the coordinates _col row_ of the score.

: .score ( -- ) score-xy at-xy score @ .(score) ;
  \ Display the score.

: record$ ( -- ca len ) s" Record: " ;
  \ Return the record label _ca len_.

wall-max-x 1- digits - record$ nip - constant record-label-x
status-y                             constant record-label-y
  \ Coordinates of the record label.

: .record$ ( -- )
  text-attr ?attr!
  record-label-x record-label-y at-xy record$ type ;
  \ Display the record label.

: record-xy ( -- col row )
  record-label-x record$ nip + record-label-y ;
  \ Return the coordinates _col row_ of the record.

: .record ( -- ) record-xy at-xy record @ .(score) ;
  \ Display the record.

: grow ( -- ) length @ 1+ max-length min length ! ;
  \ Grow the snake.

: .(apple) ( -- ) ." Q";
  \ Just display the apple, without changing the current
  \ colors, at the current cursor position.

: .apple ( -- ) apple-attr ?attr! apple 2@ at-xy .(apple) ;
  \ Display the apple.

: coords+ ( n1 n2 col1 row1 -- col2 row2 ) rot + -rot + swap ;
  \ Update coordinates _col1 row1_ with direction _n1 n2_,
  \ resulting coordinates _col2 row2_.

: move-head ( -- ) head> @ 1- max-length mod head> ! ;

: (crawl) ( n1 n2 -- )
  head 2@ 2dup previous-head 2! move-head coords+ head 2! ;
  \ Make the snake crawl in direction _n1 n2_.

: crawl ( -- ) direction 2@ (crawl) ;
  \ Make the snake crawl in the current direction.

-1  0 2constant left
  \ Left direction (coordinate increments).

 1  0 2constant right
  \ Right direction (coordinate increments).

 0  1 2constant down
  \ Down direction (coordinate increments).

 0 -1 2constant up
  \ Up direction (coordinate increments).

: at-arena? ( col row -- f ) arena-y arena-max-y between swap
                             arena-x arena-max-x between and ;
  \ Are coordinates _col row_ at the arena?

: wall? ( -- f ) head 2@ at-arena? 0= ;
  \ Has the snake hit the wall?

: crush ( -- ) previous-head 2@ at-xy crush-attr ?attr! ." X" ;

: crossing? ( -- f )
  length @ 1 ?do  i segment cross?  if unloop true exit then
             loop false ;
  \ Is the snake crossing itself?

: apple? ( -- f ) head apple clash? ;
  \ Has the snake found the apple?

: crush? ( -- f ) wall? crossing? or ;

: .horizontal-wall ( -- ) wall-max-x 1+ wall-x ?do ." +" loop ;

: .wall ( -- )
  wall-attr ?attr!
  wall-x wall-y at-xy .horizontal-wall
  arena-max-y 1+ arena-y ?do wall-x     i at-xy ." +"
                             wall-max-x i at-xy ." +" loop
  wall-x wall-max-y at-xy .horizontal-wall ;
  \ Display the wall.

: .head ( -- ) head 2@ at-xy snake-attr ?attr! ." O" ;
  \ Display the head of the snake.

variable swallow swallow off
  \ Flag: has the snake just swallowed an apple?

: .neck ( -- ) neck 2@ at-xy snake-attr ?attr!
               swallow @ if   .(apple) swallow off
                         else ." o" then ;
  \ Display the "neck" of the snake, ie. its second segment.

: -tail ( -- ) tail 2@ at-xy space ;
  \ Delete the tail of the snake.

: .snake+ ( -- ) .head .neck -tail unhome ;
  \ Display the snake updated, ie. only the parts that change
  \ during the crawling.

: .snake ( -- )
  .head length @ 1 ?do i segment 2@ at-xy ." o" loop ;
  \ Display the whole snake.

: -status ( -- ) status-attr ?attr!
                 status-x status-y at-xy status-cols spaces ;
  \ Clear the status bar.

: .status ( -- ) -status .score$ .score .record$ .record ;
  \ Display the status bar.

: init-arena ( -- ) arena-attr ?attr! page
                    .wall .status .apple .snake ;
  \ Init the arena.

: init-max-length ( -- ) (max-length) to max-length ;
  \ Init the maximum length of the snake, depending on the
  \ current size of the screen.

: new-snake ( -- )
  init-max-length  initial-length length !
  head> off  cols 2/ rows 2/ head 2!
  up direction 2!  length @ 0 ?do crawl loop ;
  \ Create a new snake with default values (length, position
  \ and direction).

: init-delay ( -- ) initial-delay delay ! ;
  \ Init the delay.

: init ( -- )
  score off init-delay new-snake new-apple init-arena ;
  \ Init the game.

: dodge ( -- ) score ?1-! .score ;
  \ Decrement the score because of the dodge.

: dodge? ( n1 n2 -- ) direction 2@ rot + -rot + or 0<> ;
  \ Do new direction _n1 n2_ causes a dodge, ie. does it
  \ change the current direction to the left or to the right?

: ?dodge ( n1 n2 -- ) dodge? if dodge then ;
  \ Manage a possible dodge caused by new direction _n1 n2_.

: new-direction ( n1 n2 -- ) 2dup ?dodge direction 2! ;
  \ Set new direction _n1 n2_.

k-down  value down-key
k-left  value left-key
k-right value right-key
k-up    value up-key
bl      value pause-key
  \ Control keys.

: pause ( -- ) begin
                 begin ekey?           until false
                 begin drop ekey ekey> until
                 pause-key =
               until ;
  \ Stop the game until the pause key is pressed again.

: manage-key ( x -- )
  case
    down-key  of down  new-direction endof
    left-key  of left  new-direction endof
    right-key of right new-direction endof
    up-key    of up    new-direction endof
    pause-key of pause               endof
   endcase ;
   \ If _x_ is a supported key, manage it.  _x_ can be
   \ a keypress, a character or an extended character.

: (rudder) ( x -- ) ekey> if manage-key else drop then ;
  \ If keyboard event _x_ i a valid one (a keypress, a
  \ character or an extended character), manage it.

: rudder ( -- ) ekey? if ekey (rudder) then ;
  \ If a keyboard event is available, manage it.

: lazy ( -- ) delay @ ms ;
  \ Wait the current delay.

: faster ( -- ) delay @ acceleration - 0 max delay ! ;
  \ Decrement the delay.

: eaten ( -- ) swallow on 10 score +! .score ;
  \ Increase and display the score.

: eat ( -- ) eaten grow faster new-apple .apple ;
  \ Eat the apple.

: ?eat ( -- ) apple? if eat then ;
  \ If the apple is found, eat it.

: center-y ( -- row ) rows 2/ ;
  \ Calculate Y coordinate _row_ of the center of the
  \ screen.

: center-x ( len -- col ) cols swap - 2/ ;
  \ Calculate X coordinate _col_ of the center of the
  \ screen, for a string lenght _len_.

: center-xy ( len -- col row ) center-x center-y ;
  \ Calculate coordinates _col row_ of the center of the
  \ screen, for a string lenght _len_.

: at-center-xy ( len -- ) center-xy at-xy ;
  \ Set cursor at the center of the screen, for a string lenght
  \ _len_.

: type-center ( ca len -- ) dup at-center-xy type ;
  \ Display string _ca len_ at the center of the screen.

: +type-center ( ca len n -- )
  >r dup center-xy r> + at-xy type ;
  \ Display string _ca len_ at the center of the screen,
  \ but adding row offset _n_.

: update-record ( -- ) score @ record @ max record ! .record ;

: game-over ( -- )
  update-record
  s" **** GAME OVER **** " type-center unhome
  500 ms -keys key drop ;

: (game) ( -- ) .snake+ lazy rudder crawl ?eat ;
  \ Game cycle.

: game ( -- ) begin (game) crush? until crush game-over ;
  \ Game loop.

: version$ ( -- ca len ) s" Version " version s+ ;

: title$ ( -- ca len ) s" ooO SERPENTINO Ooo" ;

: author$ ( -- ca len ) s" programandala.net" ;

: splash-screen ( -- ) text-attr ?attr!
                       page title$ -2 +type-center
                            version$   type-center
                            author$ 2 +type-center unhome
                       2000 ms ;
  \ Display the splash screen.

: run ( -- ) begin splash-screen init game again ;
  \ Main, endless loop.

cr cr .( Type RUN to start) cr cr \ XXX TMP --

run

\ =============================================================
\ Debugging tools

false [if]

: test-on-snake ( -- )
  init
  begin key? if key bl = if exit then then
        random-xy 2dup at-xy segment? if   ." X" key drop
                                      else ." ." then
  again ;

[then]

\ =============================================================
\ Change log

\ 2017-11-22: Fork from Robert Pfeiffer's forthsnake
\ (https://github.com/robertpfeiffer/forthsnake). Change source
\ style.  Rename words. Factor. Use constants and variables.
\ Use full screen. Draw the head apart. Document. Simplify
\ handling of directions. Remove flickering. Accelerate the
\ drawing of the snake. Add an actual scoring.
\
\ 2017-11-23: Add record. Improve the scoring calculation.
\ Improve the keyboard handling to support non-movement keys.
\ Add a pause key.
