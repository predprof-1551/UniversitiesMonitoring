import {Navigate, useLocation} from "react-router-dom";
import {Button} from "../components/Button";
import {createUseStyles} from "react-jss";
import Constants from "../Constants";
import {faStar, faTreeCity, faFileExcel, faComment} from "@fortawesome/free-solid-svg-icons";
import MessagePart from "../assets/images/message-part.svg";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {useEffect, useRef, useState} from "react";
import {SubmitButton} from "../components/SubmitButton";
import {
    GetReports,
    GetService,
    SendComment,
    SendReportToService,
    SubscribeToService,
    TestService,
    UnsubscribeToService
} from "../ApiMethods";
import Swal from "sweetalert2-neutral";
import {Carousel, Container, Stack} from "react-bootstrap";
import {GenerateUUID} from "../Utils";
import {FullscreenFrame} from "../components/FullScreenFrame";
import {Query} from "../QueryHelper";
import {Loading} from "../components/Loading";
import axios from "axios";

const useStyles = createUseStyles({
    "@keyframes loading-animation": {
        from: {
            background: "#FFF"
        },
        to: {
            background: "#ededed"
        }
    },
    excelExport: {
        position: "fixed",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        left: 10,
        bottom: 10,
        background: "#005a12",
        width: 64,
        height: 64,
        "& *": {
            color: "#FFF",
            fontSize: 32
        },
        borderRadius: "50%"
    },
    testResultContainerWrapper: {
        background: "#f5f5f5",
        padding: 30,
        "& .test-result-container": {
            background: "#FFF",
            padding: 30,
            borderRadius: 20,
            "& .result-container": {
                padding: "1rem",
                borderRadius: 20,
                justifyContent: "space-between",
            }
        },
        "& .loading-container": {
            animation: "$loading-animation 1s infinite alternate"
        },
        "& .results-list": {
            maxHeight: "30vh",
            overflowY: "auto"
        }
    },
    status: {
        background: "var(--status-color)",
        borderRadius: 10,
        textShadow: "0px 0px 2px #000",
        padding: 10
    },
    serviceHeader: {
        background: Constants.brandColor,
        color: "#FFF",
        display: "flex",
        flexDirection: "column",
        minHeight: "25%",
        padding: 10,
        paddingLeft: "5vw",
        paddingRight: "5vw",
        paddingTop: "5vh",
        "& .university-name": {
            fontSize: 32
        },
        "& .action-section": {
            display: "flex",
            flexDirection: "row",
            justifyContent: "space-between"
        },
        "& .service-action-buttons": {
            display: "flex",
            flexDirection: "column",
            gap: 10
        },
        "& .service-name-with-status": {
            display: "flex",
            alignItems: "center",
            gap: 20
        },
        "& .service-name-with-status span": {
            position: "relative",
            height: "fit-content",
            verticalAlign: "bottom"
        }
    },
    serviceBody: {
        background: "#F5F5F5",
        display: "flex",
        flexDirection: "row",
        justifyContent: "space-around",
        alignItems: "baseline",
        minHeight: "75%",
        "& .title": {
            position: "sticky",
            fontSize: 32,
            top: 10,
            zIndex: 3,
            backdropFilter: "blur(10px)",
            padding: 10,
            borderRadius: "3em",
            textAlign: "center",
            verticalAlign: "middle",
            width: "fit-content"
        },
        "& .title::after": {
            borderRadius: "3em",
            background: "rgba(245,245,245,0.55)",
            position: "absolute",
            content: '""',
            width: "100%",
            height: "100%",
            zIndex: -1,
            left: 0,
            top: 0
        }
    },
    comment: {
        background: "#FFF",
        width: "100%",
        padding: 15,
        position: "relative",
        borderRadius: "0 15px 15px 15px",
        "& .comment-author": {
            color: Constants.brandColor,
            fontWeight: "bolder"
        },
        "& .comment-header": {
            display: "flex",
            flexDirection: "row",
            justifyContent: "flex-start",
            gap: 10
        },
        "& .fa-star": {
            color: "#FDD64E"
        },
        "&::before": {
            background: `url(${MessagePart})`,
            content: '""',
            position: "absolute",
            width: 33,
            height: 35,
            left: -33,
            top: -1.0001
        },
        filter: "drop-shadow(0px 0px 24px rgba(0, 0, 0, 0.25))"
    },
    commentsWrapper: {
        "& .comments-container": {
            display: "flex",
            flexDirection: "column",
            gap: 60,
            paddingLeft: 33,
            paddingTop: 33
        },
        padding: 40,
        display: "flex",
        flexDirection: "column",
        gap: 10,
        width: "30%"
    },
    starsBar: {
        background: "#0798EA",
        borderRadius: "15px 15px 0 0",
        padding: 10,
        width: "100%",
        "& .fa-star": {
            cursor: "pointer",
            width: "15%",
            height: "100%",
            color: "var(--star-color)"
        }
    },
    commentForm: {
        position: "sticky",
        top: 10,
        display: "flex",
        justifyContent: "center",
        flexDirection: "column",
        gap: 10,
        width: 300,
        "& textarea": {
            width: "100%",
            borderRadius: "0 0 15px 15px",
            outline: "none",
            padding: 10,
        }
    },
    commentFormMobile: {
        position: "fixed",
        right: 10,
        bottom: 10,
        width: 64,
        height: 64,
        borderRadius: "50%",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: Constants.brandColor,
        transition: "background 0.3s",
        cursor: "pointer",
        userSelect: "none",
        zIndex: 100000000000,
        "& *": {
            fontSize: 32,
            color: "#FFF"
        }
    },
    commentFormMobileWrapper: {
        position: "absolute",
        width: "100%",
        height: "100%",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        top: 0,
        left: 0,
        zIndex: 10000000000000,
        background: "rgba(88,88,88,0.5)"
    },
    "@media screen and (max-width: 1136px)": {
        commentsWrapper: {
            width: "100%"
        },
        serviceHeader: {
            "& .action-section": {
                flexDirection: "column",
            }
        },
        testResultContainerWrapper: {
            "& .result-container": {
                display: "flex",
                flexDirection: "column !important"
            },
            "& .header": {
                justifyContent: "center"
            },
            "& .fa-tree-city": {
                display: "none"
            }
        }
    },
});

const monthNames = ["янв", "фев", "мар", "апр",
    "май", "июн", "июл", "авг",
    "сен", "окт", "ноя", "дек"]

export function ServicePage() {
    const style = useStyles();
    const location = useLocation();
    const [service, setService] = useState(null);
    const [smthgWentWrong, setRedirect] = useState(false);

    function updateService(service) {
        setService(service);
    }

    async function smthgFall() {
        await Swal.fire({
            title: "Что-то пошло не так",
            text: "Мы переадресуем Вас на начальную страницу. Вы будете в полной безопасности",
            icon: "error",
            showConfirmButton: false,
            timer: 2000
        });

        setRedirect(true);
    }

    useEffect(() => {
        (async () => {
            try {
                setService(await GetService(location.state?.serviceId ?? Query.serviceId));
            } catch {
                await smthgFall();
            }
        })();
    }, []);

    if (smthgWentWrong) return <Navigate to="/universities-list"/>
    if (service === null) return <Loading/>
    if (service.changedStatusAt === null) return <ServiceDidntSetupped service={service}/>

    return <div className="h-100" style={{background: "#f5f5f5"}}>
        <div className={style.excelExport}>
            <a download href={`/api/services/${service.serviceId}/excel`}><FontAwesomeIcon icon={faFileExcel}/></a>
        </div>
        <ServiceHeader service={service} updateService={updateService}/>
        <TestResultContainer service={service}/>
        <ServiceBody service={service} updateService={updateService}/>
    </div>
}

function ServiceDidntSetupped({service}) {
    return <FullscreenFrame>
        <h1 className="text-center w-75">
            О сервисе <a className="text-white" href={service.url}>"{service.serviceName}"</a> ВУЗа
            "{service.universityName}" еще нет никакой информации.
            Возращайтесь сюда через 15-20 минут, когда она точно будет.
        </h1>
    </FullscreenFrame>
}

function ServiceHeader({service, updateService}) {
    const [uptime, setUptime] = useState(null);

    useEffect(() => {
        (async () => {
            try {
                const result = await axios.get(`/api/services/${service.serviceId}/uptime`);

                setUptime(result.data.uptime);
            } catch {

            }
        })();
    }, [])

    const style = useStyles();

    const changedStatusAt = new Date(service.changedStatusAt + "Z");

    async function handleClickOnSubscribeButton() {
        if (!service.isSubscribed) {
            await SubscribeToService(service.serviceId);

        } else {
            await UnsubscribeToService(service.serviceId);
        }

        updateService({
            ...service,
            isSubscribed: !service.isSubscribed
        });
    }

    async function handleClickOnReportButton() {
        await Swal.fire({
            title: `Сообщение о ${service.isOnline ? "не" : ""}доступности сервиса`,
            input: "textarea",
            inputPlaceholder: service.isOnline ? "Опишите. Что случилось? Желательно, подробно" :
                "Если Вам есть, что добавить, то пишите сюда",
            showCancelButton: true,
            confirmButtonText: "Отправить",
            cancelButtonText: "Отмена",
            confirmButtonColor: "#0798EA",
            showLoaderOnConfirm: true,
            allowOutsideClick: () => !Swal.isLoading()
        }).then(result => {
            if (!result.isConfirmed) return;

            return SendReportToService(service.serviceId, !service.isOnline, result.value)
        }).then(result => {
            if (result) {
                Swal.fire({
                    title: "Успешно. Ожидайте подтверждения Вашего обращения от администрации",
                    icon: "success",
                    showConfirmButton: false,
                    timer: 2000
                });
            } else if (result === false) {
                Swal.fire({
                    title: "Что-то пошло не так...",
                    icon: "error",
                    showConfirmButton: false,
                    timer: 2000
                });
            }
        });
    }

    async function handleClickOnReportOfflineButton() {
        await Swal.fire({
            title: `Сообщение о недоступности сервиса`,
            input: "textarea",
            inputPlaceholder: "Опишите. Что случилось? Подробно",
            showCancelButton: true,
            confirmButtonText: "Отправить",
            cancelButtonText: "Отмена",
            confirmButtonColor: "#0798EA",
            showLoaderOnConfirm: true,
            allowOutsideClick: () => !Swal.isLoading()
        }).then(result => {
            if (!result.isConfirmed) return;

            return SendReportToService(service.serviceId, false, result.value)
        }).then(result => {
            if (result) {
                Swal.fire({
                    title: `Спасибо большое за указание причины недоступности ${service.serviceName}!`,
                    icon: "success",
                    showConfirmButton: false,
                    timer: 2000
                });
            } else if (result === false) {
                Swal.fire({
                    title: "Что-то пошло не так...",
                    icon: "error",
                    showConfirmButton: false,
                    timer: 2000
                });
            }
        });
    }

    return <div className={style.serviceHeader}>
        <div className="university-name">
            <span>{service.universityName}</span>
        </div>
        <Stack direction="horizontal" className="flex-wrap justify-content-sm-around" gap={3}>
            <div className="service-name-with-status">
                <a href={service.url} className="text-white display-1 fw-bold">{service.serviceName}</a>
                <Stack className="justify-content-center" gap={2}>
                    <span className={style.status} style={{"--status-color": service.isOnline ? "#3CFB38" : "#FB4438"}}>
                        {service.isOnline ? "Онлайн" : "Офлайн"} с {formatDate(changedStatusAt)}
                    </span>
                    {
                        uptime !== null &&
                        <span title="Показывает сколько процентов времени сервис находится в сети. Больше - лучше"
                              className={style.status}
                              style={{"--status-color": `rgb(${250 * (1 - uptime)},${250 * uptime},80)`}}>
                            Uptime: {Math.round(uptime * 100)}%
                        </span>
                    }
                </Stack>
            </div>
            <Stack className="flex-grow-0 gap-2">
                <Button onClick={handleClickOnReportButton}
                        style={{background: "#1FE03C"}}>
                    Сервис {service.isOnline ? "офлайн" : "онлайн"}?
                </Button>
                {
                    !service.isOnline &&
                    <Button onClick={handleClickOnReportOfflineButton}
                            style={{background: "#EDD715"}}>
                        Вы знаете почему сервис не работает?
                    </Button>
                }
                <Button onClick={handleClickOnSubscribeButton}
                        style={{background: service.isSubscribed ? "#FF4D15" : "#9D9D9D"}}>
                    {service.isSubscribed ? "Отписаться" : "Подписаться"}
                </Button>
            </Stack>
        </Stack>
    </div>
}

function ServiceBody({service, updateService}) {
    const [screenWidth, setWidth] = useState(window.innerWidth);
    const style = useStyles();

    useEffect(() => {
        function handleResize(e) {
            setWidth(window.innerWidth);
        }

        window.addEventListener("resize", handleResize);

        return () => window.removeEventListener("resize", handleResize);
    }, []);

    return <div className={style.serviceBody}>
        {
            screenWidth == null || screenWidth > 1136 ?
                <>
                    <CommentsColumn comments={service.comments}/>
                    {!service.isOnline && <ReportsColumn service={service}/>}
                </> : <CommentsAndReportsCarousel service={service}/>
        }
        {
            screenWidth == null || screenWidth > 1136 ?
                <SendCommentForm service={service} updateService={updateService}/> :
                <SendCommentFormMobile service={service} updateService={updateService}/>
        }
    </div>;
}

function CommentsAndReportsCarousel({service}) {
    const [index, setIndex] = useState(0);

    const handleSelect = (selectedIndex, e) => {
        setIndex(selectedIndex);
    };

    return <Carousel indicators={false} interval={null} variant="dark" activeIndex={index} onSelect={handleSelect}>
        <Carousel.Item>
            <CommentsColumn comments={service.comments}/>
        </Carousel.Item>
        {
            !service.isOnline &&
            <Carousel.Item>
                <ReportsColumn service={service}/>
            </Carousel.Item>
        }
    </Carousel>;
}

function CommentsColumn({comments}) {
    const style = useStyles();

    return <div className={style.commentsWrapper}>
        <span className="title">Комментарии</span>
        <div className="comments-container">
            {
                comments.length > 0 ? comments.map(comment =>
                        <Comment key={comment.id}
                                 from={comment.author.username}
                                 content={comment.content}
                                 stars={comment.rate}
                                 addedAt={comment.addedAt}/>) :
                    <Comment key="information-message"
                             from="Администрации сайта"
                             content="Никто пока что не оценивал этот сервис. Вы можете стать первым"/>
            }
        </div>
    </div>
}

function ReportsColumn({service}) {
    const [reports, setReports] = useState(null);
    const style = useStyles();

    useEffect(() => {
        (async () => {
            setReports(await GetReports(service.serviceId));
        })();
    }, []);

    return <div className={style.commentsWrapper}>
        <span className="title">Сообщения о текущем сбое</span>
        {
            <div className="comments-container">
                {reports === null ?
                    <Comment key="information-message"
                             from="Системы"
                             content='Загружаем жалобы...'/> :
                    reports.length > 0 ? reports.map(report =>
                            report.content !== "" &&
                            <Comment key={report.id} from="Пользователя сервиса" addedAt={report.addedAt}
                                     content={report.content}/>) :
                        <Comment key="information-message"
                                 from="Администрации сайта"
                                 content='Никто еще сообщал о причинах этого сбоя. Если Вы что-нибудь знаете о нем, расскажите, пожалуйста, нажав на кнопку "Вы знаете почему сервис не работает?" 😁'/>}
            </div>
        }
    </div>
}

function SendCommentForm({reference, service, updateService, onEnded}) {
    const [rate, setRate] = useState(1);
    const style = useStyles();

    async function handleSubmit(e) {
        e.preventDefault();

        const form = e.target;
        const formData = new FormData(form);
        formData.append("rate", rate);

        const apiData = Object.fromEntries(formData.entries());

        if (apiData.content === "") {
            return;
        }

        if (await SendComment(service.serviceId, apiData)) {
            if (onEnded) onEnded();

            await Swal.fire({
                title: "Комментарий добавлен",
                icon: "success",
                timer: 1000,
                showConfirmButton: false
            });

            service.comments.push({
                id: GenerateUUID(),
                author: {
                    username: "Вас"
                },
                ...apiData,
                rate: Number.parseInt(apiData.rate)
            });

            console.log(apiData)

            updateService({
                ...service,
                comments: service.comments,
            });
        } else {
            await Swal.fire({
                title: "Что-то пошло не так...",
                icon: "error",
                timer: 1000,
                showConfirmButton: false
            });
        }
    }

    return <form className={style.commentForm} method="post" onSubmit={handleSubmit} ref={reference}>
        <div>
            <StarsBar onChange={(count) => setRate(count)}/>
            <textarea placeholder="Оставьте Ваше мнение о сервисе здесь" name="content" rows={10} cols={1}/>
        </div>
        <SubmitButton content="Отправить"/>
    </form>;
}

function StarsBar({onChange}) {
    const style = useStyles();
    const [countOfStars, setCountOfStars] = useState(1);

    useEffect(() => {
        onChange(countOfStars);
    }, [countOfStars])

    return <div className={style.starsBar}>
        <RateStar index={1} rate={countOfStars} onClick={() => setCountOfStars(1)}/>
        <RateStar index={2} rate={countOfStars} onClick={() => setCountOfStars(2)}/>
        <RateStar index={3} rate={countOfStars} onClick={() => setCountOfStars(3)}/>
        <RateStar index={4} rate={countOfStars} onClick={() => setCountOfStars(4)}/>
        <RateStar index={5} rate={countOfStars} onClick={() => setCountOfStars(5)}/>
    </div>;
}

function RateStar({index, rate, onClick}) {
    return <FontAwesomeIcon onClick={onClick} icon={faStar}
                            style={{"--star-color": index <= rate ? "#FDD64E" : "#046298"}}/>
}

function Comment({from, content, addedAt = null, stars = -1}) {
    const style = useStyles();

    return <div className={style.comment}>
        <div className="comment-header">
            <span>
                от <span className="comment-author">{from}</span> {addedAt !== null &&
                <span className="text-muted">{formatDate(new Date(addedAt + "Z"))}</span>}
            </span>
            {
                stars > 0 &&
                <div>{[...Array(stars).keys()].map(_ => <FontAwesomeIcon icon={faStar}/>)}</div>
            }
        </div>
        <div className="comment-content">
            <p>{content}</p>
        </div>
    </div>
}

function SendCommentFormMobile({service, updateService}) {
    const style = useStyles();
    const [showPopup, setShowPopup] = useState(false);

    if (showPopup) return <SendCommentFormPopup service={service}
                                                updateService={updateService}
                                                closePopup={() => setShowPopup(false)}/>

    return <div className={style.commentFormMobile} onClick={() => setShowPopup(true)}>
        <FontAwesomeIcon icon={faComment}/>
    </div>
}

function SendCommentFormPopup({closePopup, service, updateService}) {
    const style = useStyles();
    const formElement = useRef();

    function handleClick(e) {
        let context = e.target;

        if (context === formElement.current) return

        while (context.parentElement !== null) {
            context = context.parentElement;

            if (context === formElement.current) return;
        }

        endDialog();
    }

    function endDialog() {
        const htmlElement = document.querySelector("html");

        htmlElement.style.overflowY = null;
        closePopup()
    }

    useEffect(() => {
        let a = document.documentElement.scrollTop !== undefined ? document.documentElement : document.body;
        a.scrollTop = 0;
        const htmlElement = document.querySelector("html");

        htmlElement.style.overflowY = "hidden";
    }, [])

    return <div onClick={handleClick} className={style.commentFormMobileWrapper}>
        <SendCommentForm onEnded={() => endDialog()} service={service} reference={formElement}
                         updateService={updateService}/>
    </div>
}

function LoadingContainer() {
    return <div className="test-result-container loading-container">
        <span className="text-muted fs-3 fw-bold">Собираем информацию из других городов для Вас</span>
    </div>
}

function TestResultContainer({service}) {
    const style = useStyles();
    const [testResult, setTestResult] = useState(null);

    useEffect(() => {
        (async () => {
            setTestResult(await TestService(service.serviceId));
        })();
    }, []);

    if (testResult === null) {
        return <Container className={style.testResultContainerWrapper}>
            <LoadingContainer/>
        </Container>;
    }

    if (testResult.length === 0) return null;

    return <Container className={style.testResultContainerWrapper}>
        <div className="test-result-container">
            <h1>Статистика доступа из разных городов</h1>
            <Stack className="results-list" gap={3}>
                {
                    testResult.map(result => <Stack className="result-container"
                                                    style={{background: result.headTime !== null ? "rgb(175, 254, 159)" : "rgb(254, 159, 159)"}}
                                                    direction="horizontal">
                        <Stack direction="horizontal" className="header" gap={3}>
                            <FontAwesomeIcon icon={faTreeCity} fontSize={64} color="#FFF"/>
                            <span className="fw-bold fs-4">{result.testFrom}</span>
                        </Stack>
                        <div>
                            {result.headTime !== null &&
                                <div><span><b>Время ответа сайта:</b> {result.headTime} мс</span></div>}
                            {result.pingTime !== null &&
                                <div><span><b>Время ответа сервера:</b> {result.pingTime} мс</span></div>}
                        </div>
                    </Stack>)
                }
            </Stack>
        </div>
    </Container>
}

function formatDate(date) {
    const month = date.getMonth();

    return `${padTo2Digits(date.getHours())}:${padTo2Digits(date.getMinutes())} ${date.getDate()} ${monthNames[month]}`;
}

function padTo2Digits(num) {
    return num.toString().padStart(2, '0');
}
